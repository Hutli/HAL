#region pre-script
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Linq;
using VRageMath;
using System.Text.RegularExpressions;

namespace HALUnitTest
{
    class SolarRotator3000 : MyGridProgram
    {
        public void SetGridTerminalSystem(IMyGridTerminalSystem gridTerminalSystem)
        {
            GridTerminalSystem = gridTerminalSystem;
        }
        #endregion
        //To put your code in a PB copy from this comment...

        const double ANGLE_PRECISION = 0.1;
        const int STATOR_TORQUE = 1000000;
        const int STATOR_BREAKING_TORQUE = 1000;
        const int STATOR_SLOW_RPM = 1;
        const string NAME_LIKE = "[SR3000]";
        const UpdateFrequency UPDATE_FREQUENCY = UpdateFrequency.Update1;

        public Program()
        {
            // Configure this program to run the Main method every 100 update ticks
            Runtime.UpdateFrequency = UPDATE_FREQUENCY;
        }

        private double? currentAimAngle = null;
        private double lastPowerOutput = 0.0;d

        public enum CycleStatus {
            /// Sun is down, to rise
            SunToRise,
            SunIsRising,
            Rotating
        }

        private CycleStatus cycleStatus = SunToRise;

        private Logger logger;

        public void Main(string argument)
        {
            var lcds = FindBlocksOfType<IMyTextPanel>(NAME_LIKE);
            if (lcds == null)
            {
                return;
            }

            logger = new Logger(lcds);

            cycleStatus = InferCycleStatus();
            logger.Log($"sunCycleStatus: {cycleStatus}");
            ReactOnSunCycleStatus(cycleStatus);

            //double angleDegress;
            //if (double.TryParse(argument, out angleDegress) && angleDegress <= 90 && angleDegress >= -90)
            //{
            //    Runtime.UpdateFrequency = UPDATE_FREQUENCY;
            //    currentAimAngle = angleDegress;
            //}

            //if (currentAimAngle != null)
            //{
                
            //    bool isDone = RotateToAngle(stators, currentAimAngle.Value);
            //    if (isDone)
            //    {
            //        Runtime.UpdateFrequency = UpdateFrequency.None;
            //    }
            //}
        }

        private void ReactOnSunCycleStatus(CycleStatus cycleStatus)
        {
            var solarPanels = FindBlocksOfType<IMySolarPanel>(NAME_LIKE);
            var stators = FindBlocksOfType<IMyMotorStator>(NAME_LIKE);
            switch (cycleStatus) {
                case CycleStatus.SunToRise:
                    // reset back to 0 degrees, so we are ready for sunrise
                    RotateToAngle(stators, 0);
                    break;
                case CycleStatus.SunIsRising:
                    // We want to align with the sun, if the power output is not increasing
                    var curPowerOutput = solarPanels[0].CurrentOutput;
                    if (curPowerOutput < lastPowerOutput) {
                        // change 1 degree, following the sun
                        IncreaseRotation(stators, 1);
                    }

                    lastPowerOutput = curPowerOutput;
                    break;
            }
        }

        private CycleStatus InferCycleStatus() {
            var stators = FindBlocksOfType<IMyMotorStator>(NAME_LIKE);
            var solarPanels = FindBlocksOfType<IMySolarPanel>(NAME_LIKE);

            var currentPanelOutput = solarPanels[0].CurrentOutput;

            var currentStatorAngleDeg = RadiansToDegrees(stators[0].Angle);
            // if we are at the end of the sun cycle(>= 85)
            if (currentStatorAngleDeg >= 85)
            {
                return CycleStatus.SunToRise;
            }

            // if we are waiting for the sun to rise, and we detect power generation, the sun must be rising
            if (cycleStatus == CycleStatus.SunToRise && currentPanelOutput >= 0) {
                return CycleStatus.SunIsRising;
            }

            // no other case than the current is detected
            return cycleStatus;
        }

        private double DegreesToRadians(double degrees) {
            return (Math.PI / 180) * degrees;
        }

        private double RadiansToDegrees(double radians)
        {
            return (180 / Math.PI) * radians;
        }

        /// returns whether we have reached our aim angle
        private bool RotateToAngle(IEnumerable<IMyMotorStator> stators, double angle)
        {
            cycleStatus = CycleStatus.Rotating;
            var returnBool = true;
            foreach(var x in stators)
            {
                if (!IsAngleCloseEnough(angle, RadiansToDegrees(x.Angle)))
                {
                    x.RotorLock = false;
                    x.Torque = STATOR_TORQUE;
                    x.TargetVelocityRPM = x.Angle > angle ? -STATOR_SLOW_RPM : STATOR_SLOW_RPM;
                    returnBool = false;
                }
                else
                {
                    x.RotorLock = true;
                    x.BrakingTorque = STATOR_BREAKING_TORQUE;
                    x.TargetVelocityRPM = 0;
                    x.BrakingTorque = STATOR_BREAKING_TORQUE;
                }
            }
            return returnBool;
        }

        private void IncreaseRotation(IEnumerable<IMyMotorStator> stators, double increaseByDeg) {
            var curAngleDeg = RadiansToDegrees(stators[0].Angle);
            RotateToAngle(stators, curAngleDeg + increaseByDeg);
        }

        public List<T> FindBlocksOfType<T>(string nameLike) where T : class
        {
            List<T> blocks = new List<T>();

            GridTerminalSystem.GetBlocksOfType<T>(blocks);

            return blocks.Where(x => ((IMyTerminalBlock)x).CustomName.Contains(nameLike)).ToList();
        }

        private bool IsAngleCloseEnough(double aimAngle, double currentAngle)
        {
            var angleDiff = Math.Abs(aimAngle - currentAngle);
            logger.Log($"Aim Angle: {aimAngle}");
            logger.Log($"Current Angle: {currentAngle}");
            return angleDiff <= ANGLE_PRECISION || angleDiff >= (360 - ANGLE_PRECISION);
        }
        
        public class Logger{
            private List<IMyTextPanel> _panels;
        
            public Logger(List<IMyTextPanel> panels){
                foreach(var panel in panels){
                    panel.WritePublicText("", append: false);
                }
                _panels = panels;
            }

            public void Log(string message){
                foreach(var lcd in _panels){
                    lcd.WritePublicText(message + "\n", append:true);
                }
            }
        }
#region post-cript
    }
}
#endregion