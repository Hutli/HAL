using System;
using Sandbox.ModAPI.Ingame;

namespace HALUnitTest
{
    internal class MyGridProgramRuntimeInfoMock : IMyGridProgramRuntimeInfo
    {
        private readonly DateTime _lastRun = DateTime.UtcNow;

        public TimeSpan TimeSinceLastRun => DateTime.Now.Subtract(_lastRun);

        public double LastRunTimeMs { get; }
        public int MaxInstructionCount { get; }
        public int CurrentInstructionCount { get; }
        public int MaxCallChainDepth { get; }
        public int CurrentCallChainDepth { get; }
        public UpdateFrequency UpdateFrequency { get; set; }
    }
}
