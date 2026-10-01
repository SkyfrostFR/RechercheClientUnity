using System;
using System.Threading;
using UnityEngine;

namespace DT.Simulation
{
    [Serializable]
    public class Scheduler : Singleton<Scheduler>
    {
        
        public delegate void TaskHandler();
        [field: SerializeField]
        public event TaskHandler On1ms;
        [field: SerializeField]
        public event TaskHandler On2ms;
        [field: SerializeField]
        public event TaskHandler On4ms;
        [field: SerializeField]
        public event TaskHandler On6ms;
        [field: SerializeField]
        public event TaskHandler On8ms;
        [field: SerializeField]
        public event TaskHandler On10ms;

        public enum SchedulerTime
        {
            On1ms,
            On2ms,
            On4ms,
            On6ms,
            On8ms,
            On10ms
        }

        MicroTimer timer = new MicroTimer();


        private int tickPeriod = 1000; //µs
        private float tickToMs = 1.0f; //
        private int msToTick = 1;

        [SerializeField]
        private int tick = 0;
        [SerializeField]
        private float tickTimer = 0.0f;

        Thread execThread;

        /// <summary>
        /// Register Cyclic function with Time in S
        /// </summary>
        /// <param name="Function"></param>
        /// <param name="Time"></param>
        /// <exception cref="Exception"></exception>
        public void RegisterMethod(TaskHandler Function, float Time)
        {
            if(Time == 0.0f)
            {
                return;
            }
            if (Time <= 0.001f)
            {
                RegisterMethod(Function, SchedulerTime.On1ms);
            }else if(Time <= 0.002f)
            {
                RegisterMethod(Function, SchedulerTime.On2ms);
            }
            else if (Time <= 0.004f)
            {
                RegisterMethod(Function, SchedulerTime.On4ms);
            }
            else if (Time <= 0.006f)
            {
                RegisterMethod(Function, SchedulerTime.On6ms);
            }
            else if (Time <= 0.008f)
            {
                RegisterMethod(Function, SchedulerTime.On8ms);
            }
            else if (Time <= 0.01f)
            {
                RegisterMethod(Function, SchedulerTime.On10ms);
            }else
            {
                throw new Exception("Wrong period");
            }
        }

        public void RegisterMethod(TaskHandler Function, SchedulerTime msTime)
        {
            switch (msTime)
            {
                case SchedulerTime.On1ms:
                    On1ms += Function;
                    break;
                case SchedulerTime.On2ms:
                    On2ms += Function;
                    break;
                case SchedulerTime.On4ms:
                    On4ms += Function;
                    break;
                case SchedulerTime.On6ms:
                    On6ms += Function;
                    break;
                case SchedulerTime.On8ms:
                    On8ms += Function;
                    break;
                case SchedulerTime.On10ms:
                    On10ms += Function;
                    break;
                default:
                    break;
            }
        }


        private new void Awake()
        {
            base.Awake();
            timer.Interval = tickPeriod;
            timer.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(OnTickEvent);
            timer.Start();

        }



        // Better implementation : run 1 thread per period ... and set thread priority according to task 

        private void OnTickEvent(object sender, MicroTimerEventArgs timerEventArgs)
        {
            // Usefull : timerEventArgs.CallbackFunctionExecutionTime;
            tick++;
            tickTimer = timerEventArgs.TimerLateBy;


            if (tick % 1 * msToTick == 0)
            {
                if (On1ms != null) On1ms();
            }
            if (tick % 2 * msToTick == 0)
            {
                if (On2ms != null) On2ms();
            }
            if (tick % 4 * msToTick == 0)
            {
                if (On4ms != null) On4ms();
            }
            if (tick % 6 * msToTick == 0)
            {
                if (On6ms != null) On6ms();
            }
            if (tick % 8 * msToTick == 0)
            {
                if (On8ms != null) On8ms();
            }
            if (tick % 10 * msToTick == 0)
            {
                if (On10ms != null) On10ms();
            }


        }


        public float GetTime()
        {
            return tick * tickToMs / 1000.0f;
        }
    }
}


