using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sandbox.Engine.Platform;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Linq;
using System.Text.RegularExpressions;

namespace HALUnitTest.Test
{
    [TestClass]
    public class SolarRotator3000Test
    {
        [TestMethod]
        public void BuildTest()
        {
            var hal = new Program();
            
            var gridTerminalSystem = new MyGridTerminalSystem();
            var programRuntimeInfo = new MyGridProgramRuntimeInfoMock();

            hal.SetGridTerminalSystem(gridTerminalSystem);
            hal.SetRuntime(programRuntimeInfo);
            hal.Main("0");

            Assert.IsNotNull(hal);
        }

        [TestMethod]
        public void RotateFrom0to90()
        {
            var hal = new Program();

            var gridTerminalSystem = new MyGridTerminalSystem();
            var programRuntimeInfo = new MyGridProgramRuntimeInfoMock();

            IMyMotorStator stator = new MyMotorStator();
            stator.CustomName = "[SR3000 G1] Stator 1";

            try
            {
                gridTerminalSystem.Add((MyTerminalBlock)stator);
            }
            catch (InvalidCastException e)
            {
                Assert.Fail(e.Message);
            }
            hal.SetGridTerminalSystem(gridTerminalSystem);
            hal.SetRuntime(programRuntimeInfo);
            
            hal.Main("90");
            Assert.IsTrue(stator.Angle > 89 && stator.Angle < 91);
        }
    } 
}
