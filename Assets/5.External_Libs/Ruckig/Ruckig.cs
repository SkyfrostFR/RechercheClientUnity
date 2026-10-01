using System;
using System.Runtime.InteropServices;

[Serializable]
public class Ruckig
{

    private int dof;
    private double delta_time;
    private IntPtr ruckigPtr;

    InternInput_typ _Input;
    InternOutput_typ _Output;

    public Input_typ Input;
    public Output_typ Output;

    [Serializable]
    public enum ControlInterface
    {
        Position,
        Velocity,
        Acceleration,
        Jerk
    }
    [Serializable]
    public enum Synchronization
    {
        None,
        Time,
        Phase
    }
    [Serializable]
    public enum DurationDiscretization
    {
        Continuous,
        Discrete
    }
    [Serializable]
    public enum Result
    {
        Working = 0, ///< The trajectory is calculated normally
        Finished = 1, ///< The trajectory has reached its final position
        Error = -1, ///< Unclassified error
        ErrorInvalidInput = -100, ///< Error in the input parameter
        ErrorTrajectoryDuration = -101, ///< The trajectory duration exceeds its numerical limits
        ErrorPositionalLimits = -102, ///< The trajectory exceeds the given positional limits (only in Ruckig Pro)
        // ErrorNoPhaseSynchronization = -103, ///< The trajectory cannot be phase synchronized
        ErrorExecutionTimeCalculation = -110, ///< Error during the extremel time calculation (Step 1)
        ErrorSynchronizationCalculation = -111, ///< Error during the synchronization calculation (Step 2)
    };

    [Serializable]
    // Define struct types for input and output of the Update function
    public struct Input_typ
    {
        public int degrees_of_freedom;

        public double[] current_position;
        public double[] current_velocity;
        public double[] current_acceleration;

        public double[] target_position;
        public double[] target_velocity;
        public double[] target_acceleration;

        public double[] max_velocity;
        public double[] max_acceleration;
        public double[] max_jerk;

        public bool[] enabled;

        public ControlInterface control_interface;
        public Synchronization synchronization;
        public DurationDiscretization duration_discretization;

        public Input_typ(int dofs)
        {
            degrees_of_freedom = dofs;
            current_position = new double[dofs];
            current_velocity = new double[dofs];
            current_acceleration = new double[dofs];
            target_position = new double[dofs];
            target_velocity = new double[dofs];
            target_acceleration = new double[dofs];
            max_velocity = new double[dofs];
            max_acceleration = new double[dofs];
            max_jerk = new double[dofs];
            enabled = new bool[dofs];
            control_interface = ControlInterface.Position;
            synchronization = Synchronization.None;
            duration_discretization = DurationDiscretization.Continuous;

            for (int i = 0; i < dofs; i++)
            {
                enabled[i] = true;
            }
        }
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    struct InternInput_typ
    {
        public int degrees_of_freedom;

        public IntPtr current_position;
        public IntPtr current_velocity;
        public IntPtr current_acceleration;

        public IntPtr target_position;
        public IntPtr target_velocity;
        public IntPtr target_acceleration;

        public IntPtr max_velocity;
        public IntPtr max_acceleration;
        public IntPtr max_jerk;

        public IntPtr enabled;

        public ControlInterface control_interface;
        public Synchronization synchronization;
        public DurationDiscretization duration_discretization;

        public InternInput_typ(int dofs)
        {
            degrees_of_freedom = dofs;
            current_position = Marshal.AllocHGlobal(sizeof(double) * dofs);
            current_velocity = Marshal.AllocHGlobal(sizeof(double) * dofs);
            current_acceleration = Marshal.AllocHGlobal(sizeof(double) * dofs);
            target_position = Marshal.AllocHGlobal(sizeof(double) * dofs);
            target_velocity = Marshal.AllocHGlobal(sizeof(double) * dofs);
            target_acceleration = Marshal.AllocHGlobal(sizeof(double) * dofs);
            max_velocity = Marshal.AllocHGlobal(sizeof(double) * dofs);
            max_acceleration = Marshal.AllocHGlobal(sizeof(double) * dofs);
            max_jerk = Marshal.AllocHGlobal(sizeof(double) * dofs);
            enabled = Marshal.AllocHGlobal(sizeof(bool) * dofs);
            control_interface = ControlInterface.Position;
            synchronization = Synchronization.None;
            duration_discretization = DurationDiscretization.Continuous;
        }

    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    struct InternOutput_typ
    {
        public IntPtr new_position; // Pointer to double
        public IntPtr new_velocity; // Pointer to double
        public IntPtr new_acceleration; // Pointer to double

        public double time; // The current, auto-incremented time. Reset to 0 at a new calculation.

        public ulong new_section; // Index of the section between two (possibly filtered) intermediate positions.
        public bool did_section_change; // Was a new section reached in the last cycle?

        public bool new_calculation; // Whether a new calculation was performed in the last cycle
        public bool was_calculation_interrupted; // Was the trajectory calculation interrupted? (only in Pro Version)
        public double calculation_duration; // Duration of the calculation in the last cycle [µs]

        public InternOutput_typ(int dofs)
        {
            new_position = Marshal.AllocHGlobal(sizeof(double) * dofs);
            new_velocity = Marshal.AllocHGlobal(sizeof(double) * dofs);
            new_acceleration = Marshal.AllocHGlobal(sizeof(double) * dofs);
            time = 0;
            new_section = 0;
            did_section_change = false;
            new_calculation = false;
            was_calculation_interrupted = false;
            calculation_duration = 0;
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Output_typ
    {
        public double[] new_position;
        public double[] new_velocity;
        public double[] new_acceleration;

        //Trajectory trajectory; // The current trajectory

        public double time; // The current, auto-incremented time. Reset to 0 at a new calculation.

        public ulong new_section; // Index of the section between two (possibly filtered) intermediate positions.
        public bool did_section_change; // Was a new section reached in the last cycle?

        public bool new_calculation; // Whether a new calculation was performed in the last cycle
        public bool was_calculation_interrupted; // Was the trajectory calculation interrupted? (only in Pro Version)
        public double calculation_duration; // Duration of the calculation in the last cycle [µs]

        public Output_typ(int dofs)
        {
            new_position = new double[dofs];
            new_velocity = new double[dofs];
            new_acceleration = new double[dofs];

            time = 0;
            new_section = 0;
            did_section_change = false;

            new_calculation = false;
            was_calculation_interrupted = false;
            calculation_duration = 0;
        }

    }



    // Import the Ruckig constructor
    [DllImport("Ruckig.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr RuckigNew(int DOF, double delta_time);
    // Import the Update function
    [DllImport("Ruckig.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void RuckigDelete(IntPtr ruckig);
    // Import the Update function
    [DllImport("Ruckig.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Update(IntPtr ruckig, ref InternInput_typ input, ref InternOutput_typ output);

    // Import the outputToInput function
    [DllImport("Ruckig.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void OutputToInput(IntPtr ruckig, ref InternInput_typ input, ref InternOutput_typ output);
	// Update deltaTime
	[DllImport("Ruckig.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void UpdateDeltaTime(IntPtr ruckig, double deltaTime);


    public Ruckig(int DOF, double deltaTime)
    {
        dof = DOF;
        delta_time = deltaTime;
        ruckigPtr = RuckigNew(dof, delta_time);
        Input = new Input_typ(DOF);
        Output = new Output_typ(DOF);

        _Input = new InternInput_typ(DOF);
        _Output = new InternOutput_typ(DOF);
    }

    void toInternInput()
    {
        Marshal.Copy(Input.current_position, 0, _Input.current_position, dof);
        Marshal.Copy(Input.current_velocity, 0, _Input.current_velocity, dof);
        Marshal.Copy(Input.current_acceleration, 0, _Input.current_acceleration, dof);

        Marshal.Copy(Input.target_position, 0, _Input.target_position, dof);
        Marshal.Copy(Input.target_velocity, 0, _Input.target_velocity, dof);
        Marshal.Copy(Input.target_acceleration, 0, _Input.target_acceleration, dof);
        
        Marshal.Copy(Input.max_velocity, 0, _Input.max_velocity, dof);
        Marshal.Copy(Input.max_acceleration, 0, _Input.max_acceleration, dof);
        Marshal.Copy(Input.max_jerk, 0, _Input.max_jerk, dof);

        // Need to manually convert to byte array 
        byte[] byteArray = new byte[dof];
        Buffer.BlockCopy(Input.enabled, 0, byteArray, 0, dof);
        Marshal.Copy(byteArray, 0, _Input.enabled, dof);

        _Input.control_interface = Input.control_interface;
        _Input.synchronization = Input.synchronization;
        _Input.duration_discretization = Input.duration_discretization;

    }
    void toPublicInput()
    {
        Marshal.Copy(_Input.current_position,Input.current_position,0, dof);
        Marshal.Copy(_Input.current_velocity,Input.current_velocity, 0, dof);
        Marshal.Copy(_Input.current_acceleration, Input.current_acceleration, 0, dof);
                     
        Marshal.Copy(_Input.target_position, Input.target_position, 0, dof);
        Marshal.Copy(_Input.target_velocity, Input.target_velocity, 0, dof);
        Marshal.Copy(_Input.target_acceleration, Input.target_acceleration, 0, dof);
                     
        Marshal.Copy(_Input.max_velocity, Input.max_velocity, 0, dof);
        Marshal.Copy(_Input.max_acceleration, Input.max_acceleration, 0, dof);
        Marshal.Copy(_Input.max_jerk, Input.max_jerk, 0, dof);

        // Need to manually convert to byte array 
        byte[] byteArray = new byte[dof];
        Marshal.Copy(_Input.enabled, byteArray, 0, dof);
        Buffer.BlockCopy(byteArray, 0, Input.enabled, 0, dof);


        Input.control_interface = _Input.control_interface;
        Input.synchronization = _Input.synchronization;
        Input.duration_discretization = _Input.duration_discretization;

    }
    void toPublicOutput()
    {
        Marshal.Copy(_Output.new_position, Output.new_position, 0, dof);
        Marshal.Copy(_Output.new_velocity, Output.new_velocity, 0, dof);
        Marshal.Copy(_Output.new_acceleration, Output.new_acceleration, 0, dof);

        Output.time = _Output.time;
        Output.new_section = _Output.new_section;
        Output.did_section_change = _Output.did_section_change;

        Output.new_calculation = _Output.new_calculation;
        Output.was_calculation_interrupted = _Output.was_calculation_interrupted;
        Output.calculation_duration = _Output.calculation_duration;
    }
    void toInternOutput()
    {
        Marshal.Copy(Output.new_position, 0, _Output.new_position, dof);
        Marshal.Copy(Output.new_velocity, 0, _Output.new_velocity, dof);
        Marshal.Copy(Output.new_acceleration, 0, _Output.new_acceleration, dof);

        _Output.time = Output.time;
        _Output.new_section = Output.new_section;
        _Output.did_section_change = Output.did_section_change;
        
        _Output.new_calculation = Output.new_calculation;
        _Output.was_calculation_interrupted = Output.was_calculation_interrupted;
        _Output.calculation_duration = Output.calculation_duration;
    }

    public Result Update()
    {
        if (ruckigPtr != IntPtr.Zero)
        {
            // Copy public struct to internal
            toInternInput();
            // Update
            Result res = (Result)Update(ruckigPtr, ref _Input, ref _Output);
            
            // Copy internal to public struct
            toPublicOutput();
            return res;
        }
        else
        {
            return Result.Error;
        }
    }

    public void OutputToInput()
    {
        if (ruckigPtr != IntPtr.Zero)
        {
            // Copy public struct to internal
            toInternOutput();
            // Update
            OutputToInput(ruckigPtr, ref _Input, ref _Output);
            
            toPublicInput();
        }
    }
	
	public void UpdateDeltaTime(double deltaTime)
    {
        if (ruckigPtr != IntPtr.Zero)
        {
           
            UpdateDeltaTime(ruckigPtr, deltaTime);
            delta_time = deltaTime;
        }
    }


    public void Dispose()
    {
        RuckigDelete(ruckigPtr);
        ruckigPtr = IntPtr.Zero;
    }

    ~Ruckig()
    {
        Dispose();
    }

}
