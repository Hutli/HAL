using Microsoft.VisualStudio.TestTools.UnitTesting;
using IngameScript;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities;
using SpaceEngineers.Game.ModAPI.Ingame;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.Entities.Weapons;
using Sandbox.Definitions;
using SpaceEngineers.Game.Entities.Cube;
using VRage.Game.Entity;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;

namespace HALUnitTest {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void MainInEmptyWorld() {
            Program HAL = new Program();
            MyGridTerminalSystem GridTerminalSystem = new MyGridTerminalSystem();
                       
            HAL.SetGridTerminalSystem(GridTerminalSystem);
            HAL.Main("");
        }

        /*public void MainOnIrrelevantGrid() {
            List<MyTerminalBlock> blocksBefore = new List<MyTerminalBlock>();
            Myob
            MyObjectBuilder_FloatingObject builder = new MyObjectBuilder_FloatingObject();
            builder.

            innerDoor.SetCustomName("AL1 Inner Door [HAL AL1:ID]");
            blocksBefore.Add(innerDoor);
            MyDoor outerDoor = new MyDoor();
            innerDoor.SetCustomName("AL1 Outer Door [HAL AL1:OD]");
            blocksBefore.Add(outerDoor);
        }*/
    }
}
