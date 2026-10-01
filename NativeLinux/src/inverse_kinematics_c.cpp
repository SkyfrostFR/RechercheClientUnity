// Linux implementation of the C API that
// Assets/5.External_Libs/InverseKinematics/InverseKinematics.cs imports from
// "InverseKinematics.dll" (the shipped DLL is Windows-only, without sources).
//
// Velocity-level (differential) IK, one call per frame:
//   minimise ||J dq - dX||^2 + damping^2 ||dq||^2 + w ||dq - (q0 - q)||^2
//   QP   : subject to joint position, velocity and acceleration bounds on dq
//   DLS  : unconstrained, then scaled to the velocity limits
//   PINV : DLS with a negligible damping
// then q += dq * dt.
//
// Units, as TiagoDualArmIK.cs fills the input: q, q0 in degrees, dq in deg/s,
// limits in deg, deg/s and deg/s^2; the Jacobian comes from PhysX
// (GetDenseJacobian), i.e. per radian, opDOF x jointDOF stored column-major
// (MathsExtensions.Flatten, Eigen's default layout); dX in m/s
// and rad/s. This is a reimplementation: results follow the same model as the
// original solver but are not bit-identical to it.
#include <algorithm>
#include <chrono>
#include <cmath>
#include <cstdint>
#include <vector>

namespace {

constexpr double kDeg = M_PI / 180.0;

enum Solver : int32_t { QP = 0, DLS = 1, PINV = 2 };
enum Status : int32_t { NONE = 0, INITIATED = 1, RESULT_OK = 2, ERROR = 3, NOT_IMPLEMENTED = 4 };

struct InternConstraints { double *qmin, *qmax, *dqmax, *ddqmax; };

struct InternInput {
    double *jacobian, *dx_desired, *q, *dq, *q0;
    double delta_time, dls_damping, regularization_weight;
    int32_t enable_regularization;  // C# bool marshals as a 4-byte BOOL
};

struct InternOutput {
    int32_t status;
    double computation_time;
    double *dq, *q;
};

struct Handle {
    int n, m;
    Solver solver;
    std::vector<double> qmin, qmax, dqmax, ddqmax;  // degrees
    bool has_constraints = false;
};

// Solves A x = b for a symmetric positive definite A (n <= ~10), in place.
bool cholesky_solve(std::vector<double> a, std::vector<double>& b, int n) {
    for (int j = 0; j < n; ++j) {
        double d = a[j * n + j];
        for (int k = 0; k < j; ++k) d -= a[j * n + k] * a[j * n + k];
        if (d <= 1e-15) return false;
        d = std::sqrt(d);
        a[j * n + j] = d;
        for (int i = j + 1; i < n; ++i) {
            double s = a[i * n + j];
            for (int k = 0; k < j; ++k) s -= a[i * n + k] * a[j * n + k];
            a[i * n + j] = s / d;
        }
    }
    for (int i = 0; i < n; ++i) {
        double s = b[i];
        for (int k = 0; k < i; ++k) s -= a[i * n + k] * b[k];
        b[i] = s / a[i * n + i];
    }
    for (int i = n - 1; i >= 0; --i) {
        double s = b[i];
        for (int k = i + 1; k < n; ++k) s -= a[k * n + i] * b[k];
        b[i] = s / a[i * n + i];
    }
    return true;
}

// min 1/2 x'Hx - g'x  s.t. lo <= x <= hi, by a primal active-set loop
// (each pass fixes the most violated free variable at its bound).
bool box_qp(const std::vector<double>& H, const std::vector<double>& g,
            const std::vector<double>& lo, const std::vector<double>& hi,
            std::vector<double>& x, int n) {
    std::vector<int> state(n, 0);  // 0 free, -1 at lo, +1 at hi
    for (int pass = 0; pass <= n; ++pass) {
        std::vector<int> free;
        for (int i = 0; i < n; ++i) {
            if (state[i] == 0) free.push_back(i);
            else x[i] = state[i] < 0 ? lo[i] : hi[i];
        }
        const int f = static_cast<int>(free.size());
        if (f > 0) {
            std::vector<double> Hf(f * f), rhs(f);
            for (int a = 0; a < f; ++a) {
                double r = g[free[a]];
                for (int i = 0; i < n; ++i)
                    if (state[i] != 0) r -= H[free[a] * n + i] * x[i];
                rhs[a] = r;
                for (int b = 0; b < f; ++b) Hf[a * f + b] = H[free[a] * n + free[b]];
            }
            if (!cholesky_solve(Hf, rhs, f)) return false;
            for (int a = 0; a < f; ++a) x[free[a]] = rhs[a];
        }
        int worst = -1;
        double worst_violation = 1e-12;
        for (int i : free) {
            const double v = std::max(lo[i] - x[i], x[i] - hi[i]);
            if (v > worst_violation) { worst_violation = v; worst = i; }
        }
        if (worst < 0) return true;
        state[worst] = x[worst] < lo[worst] ? -1 : 1;
    }
    for (int i = 0; i < n; ++i) x[i] = std::clamp(x[i], lo[i], hi[i]);
    return true;
}

}  // namespace

extern "C" {

void* IKNew(int joint_dof, int op_dof, int solver) {
    if (joint_dof <= 0 || op_dof <= 0) return nullptr;
    auto* h = new Handle();
    h->n = joint_dof;
    h->m = op_dof;
    h->solver = static_cast<Solver>(solver);
    return h;
}

void IKDelete(void* ptr) { delete static_cast<Handle*>(ptr); }

void IKSetConstraints(void* ptr, InternConstraints* c) {
    auto* h = static_cast<Handle*>(ptr);
    if (!h || !c) return;
    h->qmin.assign(c->qmin, c->qmin + h->n);
    h->qmax.assign(c->qmax, c->qmax + h->n);
    h->dqmax.assign(c->dqmax, c->dqmax + h->n);
    h->ddqmax.assign(c->ddqmax, c->ddqmax + h->n);
    h->has_constraints = true;
}

int Update(void* ptr, InternInput* in, InternOutput* out) {
    auto* h = static_cast<Handle*>(ptr);
    if (!h || !in || !out) return ERROR;
    const auto t0 = std::chrono::steady_clock::now();
    const int n = h->n, m = h->m;
    const double dt = in->delta_time > 0 ? in->delta_time : 1e-3;

    std::vector<double> q(n), dq_prev(n);
    for (int i = 0; i < n; ++i) {
        q[i] = in->q[i] * kDeg;
        dq_prev[i] = in->dq[i] * kDeg;
    }

    // Normal equations: H = J'J + lambda^2 I (+ w I),  g = J'dX (+ w (q0 - q))
    const double lambda = h->solver == PINV ? 1e-6 : std::max(in->dls_damping, 1e-6);
    std::vector<double> H(n * n, 0.0), g(n, 0.0);
    for (int i = 0; i < n; ++i) {
        for (int j = 0; j < n; ++j) {
            double s = 0;
            for (int r = 0; r < m; ++r) s += in->jacobian[i * m + r] * in->jacobian[j * m + r];
            H[i * n + j] = s;
        }
        H[i * n + i] += lambda * lambda;
        for (int r = 0; r < m; ++r) g[i] += in->jacobian[i * m + r] * in->dx_desired[r];
    }
    if (in->enable_regularization && in->regularization_weight > 0) {
        const double w = in->regularization_weight;
        for (int i = 0; i < n; ++i) {
            H[i * n + i] += w;
            g[i] += w * (in->q0[i] * kDeg - q[i]);
        }
    }

    // Bounds on dq (rad/s) from the joint limits, when they are set.
    std::vector<double> lo(n, -1e9), hi(n, 1e9);
    if (h->has_constraints) {
        for (int i = 0; i < n; ++i) {
            if (h->dqmax[i] > 0) {
                lo[i] = -h->dqmax[i] * kDeg;
                hi[i] = h->dqmax[i] * kDeg;
            }
            if (h->qmax[i] > h->qmin[i]) {
                lo[i] = std::max(lo[i], (h->qmin[i] * kDeg - q[i]) / dt);
                hi[i] = std::min(hi[i], (h->qmax[i] * kDeg - q[i]) / dt);
            }
            if (h->ddqmax[i] > 0) {
                lo[i] = std::max(lo[i], dq_prev[i] - h->ddqmax[i] * kDeg * dt);
                hi[i] = std::min(hi[i], dq_prev[i] + h->ddqmax[i] * kDeg * dt);
            }
            if (lo[i] > hi[i]) lo[i] = hi[i] = 0.5 * (lo[i] + hi[i]);
        }
    }

    std::vector<double> dq(n, 0.0);
    bool ok;
    if (h->solver == QP) {
        ok = box_qp(H, g, lo, hi, dq, n);
    } else {
        dq = g;
        ok = cholesky_solve(H, dq, n);
        if (ok) {  // keep the direction, scale down to the tightest bound
            double scale = 1.0;
            for (int i = 0; i < n; ++i) {
                if (dq[i] > hi[i] && dq[i] > 0) scale = std::min(scale, hi[i] / dq[i]);
                if (dq[i] < lo[i] && dq[i] < 0) scale = std::min(scale, lo[i] / dq[i]);
            }
            for (double& v : dq) v *= std::max(scale, 0.0);
        }
    }

    out->status = ok ? RESULT_OK : ERROR;
    if (ok) {
        for (int i = 0; i < n; ++i) {
            out->dq[i] = dq[i] / kDeg;
            out->q[i] = (q[i] + dq[i] * dt) / kDeg;
        }
    }
    out->computation_time =
        std::chrono::duration<double>(std::chrono::steady_clock::now() - t0).count();
    return out->status;
}

}  // extern "C"
