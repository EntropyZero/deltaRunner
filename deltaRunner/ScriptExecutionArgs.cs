using System;

namespace EntropyZero.deltaRunner
{
    public class ScriptExecutionArgs : EventArgs
    {
        public readonly TimeSpan Duration;
        public readonly DeltaFile Delta;

        public ScriptExecutionArgs(DeltaFile delta)
        {
            Duration = TimeSpan.MinValue;
            Delta = delta;
        }

        public ScriptExecutionArgs(DeltaFile delta, TimeSpan duration)
        {
            Duration = duration;
            Delta = delta;
        }
    }
}