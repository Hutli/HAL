using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sandbox.Game.GameSystems;

namespace HALUnitTest {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void MainInEmptyWorld() {
            var hal = new Program();
            var gridTerminalSystem = new MyGridTerminalSystem();

            hal.SetGridTerminalSystem(gridTerminalSystem);
            hal.Main();
        }
    }
}