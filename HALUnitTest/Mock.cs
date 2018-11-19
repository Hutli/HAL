using System.Diagnostics.Eventing.Reader;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using IMyMotorStator = Sandbox.ModAPI.IMyMotorStator;

namespace HALUnitTest
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorStator))]
    class Mock : MyGameLogicComponent
    {
        private IMyMotorStator Stator;

        public override void Close()
        {
            base.Close();
        }

        void statorStateChanged(bool obj)
        {
            Sandbox.ModAPI.MyAPIGateway.Entities.
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Stator = Entity as IMyMotorStator;
        }

        public override void MarkForClose()
        {
            base.MarkForClose();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
        }
    }
}
