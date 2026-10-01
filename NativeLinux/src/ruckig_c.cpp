// Linux build of the C API that Assets/5.External_Libs/Ruckig/Ruckig.cs imports
// from "Ruckig.dll" (the shipped DLL is Windows-only). Wraps the open-source
// Ruckig library (https://github.com/pantor/ruckig).
//
// The structs mirror Ruckig.cs's InternInput_typ / InternOutput_typ with
// LayoutKind.Sequential: C# enums are int32, C# bool fields marshal as 4-byte
// BOOL, and `enabled` is a byte array written with Buffer.BlockCopy.
#include <chrono>
#include <cstdint>
#include <memory>

#include <ruckig/ruckig.hpp>

namespace {

struct InternInput {
    int32_t degrees_of_freedom;
    double *current_position, *current_velocity, *current_acceleration;
    double *target_position, *target_velocity, *target_acceleration;
    double *max_velocity, *max_acceleration, *max_jerk;
    uint8_t *enabled;
    int32_t control_interface;       // Position, Velocity, Acceleration, Jerk
    int32_t synchronization;         // None, Time, Phase
    int32_t duration_discretization; // Continuous, Discrete
};

struct InternOutput {
    double *new_position, *new_velocity, *new_acceleration;
    double time;
    uint64_t new_section;
    int32_t did_section_change;
    int32_t new_calculation;
    int32_t was_calculation_interrupted;
    double calculation_duration;
};

using Otg = ruckig::Ruckig<ruckig::DynamicDOFs>;

struct Handle {
    size_t dofs;
    std::unique_ptr<Otg> otg;
    ruckig::InputParameter<ruckig::DynamicDOFs> input;
    ruckig::OutputParameter<ruckig::DynamicDOFs> output;

    Handle(size_t n, double dt) : dofs(n), otg(new Otg(n, dt)), input(n), output(n) {}
};

ruckig::Synchronization to_sync(int32_t s) {
    switch (s) {
        case 1: return ruckig::Synchronization::Time;
        case 2: return ruckig::Synchronization::Phase;
        default: return ruckig::Synchronization::None;
    }
}

}  // namespace

extern "C" {

void* RuckigNew(int dof, double delta_time) {
    if (dof <= 0) return nullptr;
    return new Handle(static_cast<size_t>(dof), delta_time);
}

void RuckigDelete(void* ptr) {
    delete static_cast<Handle*>(ptr);
}

int Update(void* ptr, InternInput* in, InternOutput* out) {
    auto* h = static_cast<Handle*>(ptr);
    if (!h || !in || !out) return static_cast<int>(ruckig::Result::Error);
    // Acceleration / Jerk control interfaces only exist in Ruckig Pro.
    if (in->control_interface > 1) return static_cast<int>(ruckig::Result::ErrorInvalidInput);

    auto& p = h->input;
    for (size_t i = 0; i < h->dofs; ++i) {
        p.current_position[i] = in->current_position[i];
        p.current_velocity[i] = in->current_velocity[i];
        p.current_acceleration[i] = in->current_acceleration[i];
        p.target_position[i] = in->target_position[i];
        p.target_velocity[i] = in->target_velocity[i];
        p.target_acceleration[i] = in->target_acceleration[i];
        p.max_velocity[i] = in->max_velocity[i];
        p.max_acceleration[i] = in->max_acceleration[i];
        p.max_jerk[i] = in->max_jerk[i];
        p.enabled[i] = in->enabled[i] != 0;
    }
    p.control_interface = in->control_interface == 1 ? ruckig::ControlInterface::Velocity
                                                     : ruckig::ControlInterface::Position;
    p.synchronization = to_sync(in->synchronization);
    p.duration_discretization = in->duration_discretization == 1
        ? ruckig::DurationDiscretization::Discrete : ruckig::DurationDiscretization::Continuous;

    const auto result = h->otg->update(p, h->output);

    const auto& o = h->output;
    for (size_t i = 0; i < h->dofs; ++i) {
        out->new_position[i] = o.new_position[i];
        out->new_velocity[i] = o.new_velocity[i];
        out->new_acceleration[i] = o.new_acceleration[i];
    }
    out->time = o.time;
    out->new_section = o.new_section;
    out->did_section_change = o.did_section_change ? 1 : 0;
    out->new_calculation = o.new_calculation ? 1 : 0;
    out->was_calculation_interrupted = o.was_calculation_interrupted ? 1 : 0;
    out->calculation_duration = o.calculation_duration;
    return static_cast<int>(result);
}

// Same as OutputParameter::pass_to_input: the new state becomes the current one.
void OutputToInput(void* ptr, InternInput* in, InternOutput* out) {
    auto* h = static_cast<Handle*>(ptr);
    if (!h || !in || !out) return;
    for (size_t i = 0; i < h->dofs; ++i) {
        in->current_position[i] = out->new_position[i];
        in->current_velocity[i] = out->new_velocity[i];
        in->current_acceleration[i] = out->new_acceleration[i];
    }
}

void UpdateDeltaTime(void* ptr, double delta_time) {
    auto* h = static_cast<Handle*>(ptr);
    if (h) h->otg->delta_time = delta_time;
}

}  // extern "C"
