using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sandbox.Game.GameSystems;

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

            hal.SetGridTerminalSystem(gridTerminalSystem);
            hal.Main();
        }
    }
}
