#region pre-script
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Linq;
using System.Text.RegularExpressions;

namespace HALUnitTest
{
    internal sealed class Program : MyGridProgram
    {
        public void SetGridTerminalSystem(IMyGridTerminalSystem gridTerminalSystem)
        {
            GridTerminalSystem = gridTerminalSystem;
        }
        #endregion
        //To put your code in a PB copy from this comment...

        private const double AnglePrecision = 0.1;
        private const int StatorTorque = 1000000;
        private const int StatorBreakingTorque = 1000;
        private const int StatorSlowRpm = 1;
        private const string NameLike = "[SR3000";
        private const double PlanetMinAngle = -85;
        private const double PlanetMaxAngle = 85;
        private const UpdateFrequency ErrorUpdateFrequency = UpdateFrequency.None;
        private const UpdateFrequency IdleUpdateFrequency = UpdateFrequency.Update100;
        private const UpdateFrequency WorkingUpdateFrequency = UpdateFrequency.Update1;
        private readonly SolarFarm _solarFarm;
        private readonly Logger _logger;

        public Program()
        {
            // Configure this program to run the Main method every 100 update ticks
            Runtime.UpdateFrequency = IdleUpdateFrequency;
            _solarFarm = new SolarFarm(FindBlocksOfType<IMyMotorStator>(NameLike), FindBlocksOfType<IMySolarPanel>(NameLike), PlanetMinAngle, PlanetMaxAngle);
            _logger = new Logger(FindBlocksOfType<IMyTextPanel>(NameLike));
        }

        //private double? currentAimAngle = null;
        //private double lastPowerOutput = 0.0;

        /*public enum CycleStatus {
            /// Sun is down, to rise
            SunToRise,
            SunIsRising,
            Rotating
        }*/

        //private CycleStatus cycleStatus = SunToRise;

        public void Main(string argument)
        {
            var solarFarmState = _solarFarm.Run();
            _logger.Clear();
            _logger.Log($"Solar Farm State: {solarFarmState}");
            switch (solarFarmState)
            {
                case SolarFarm.SolarFarmState.Ready:
                    Runtime.UpdateFrequency = IdleUpdateFrequency;
                    break;
                case SolarFarm.SolarFarmState.Rotating:
                    Runtime.UpdateFrequency = WorkingUpdateFrequency;
                    break;
                default:
                    Runtime.UpdateFrequency = ErrorUpdateFrequency;
                    _logger.Log("Unknown state, terminating execution");
                    break;
            }

            //cycleStatus = InferCycleStatus();
            //logger.Log($"sunCycleStatus: {cycleStatus}");
            //ReactOnSunCycleStatus(cycleStatus);

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

        /*private void ReactOnSunCycleStatus(CycleStatus cycleStatus)
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

        public double DegreesToRadians(double degrees) {
            return (Math.PI / 180) * degrees;
        }

        public double RadiansToDegrees(double radians)
        {
            return (180 / Math.PI) * radians;
        }*/

        /// returns whether we have reached our aim angle
        /*private bool RotateToAngle(IEnumerable<IMyMotorStator> stators, double angle)
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
        }*/

        //private void IncreaseRotation(IEnumerable<IMyMotorStator> stators, double increaseByDeg) {
        //    var curAngleDeg = RadiansToDegrees(stators[0].Angle);
        //    RotateToAngle(stators, curAngleDeg + increaseByDeg);
        //}
        private List<T> FindBlocksOfType<T>(string nameLike) where T : class
        {
            var blocks = new List<T>();

            GridTerminalSystem.GetBlocksOfType(blocks);

            return blocks.Where(x => ((IMyTerminalBlock)x).CustomName.Contains(nameLike)).ToList();
        }

        //private bool IsAngleCloseEnough(double aimAngle, double currentAngle)
        //{
        //    var angleDiff = Math.Abs(aimAngle - currentAngle);
        //    logger.Log($"Aim Angle: {aimAngle}");
        //    logger.Log($"Current Angle: {currentAngle}");
        //    return angleDiff <= ANGLE_PRECISION || angleDiff >= (360 - ANGLE_PRECISION);
        //}
        
        public class SolarFarm
        {
            private readonly IEnumerable<SolarFarmArm> _solarFarmArms;

            public enum SolarFarmState
            {
                Ready,
                Rotating
            }

            public SolarFarm(IEnumerable<IMyMotorStator> stators, IEnumerable<IMySolarPanel> solarPanels, double minAngle, double maxAngle)
            {
                _solarFarmArms = CreateArms(stators, solarPanels, minAngle, maxAngle);
            }

            public SolarFarmState Run()
            {
                return _solarFarmArms.Aggregate(SolarFarmState.Ready, (worker, next) => next.Run() == SolarFarmArm.SolarFarmArmState.Rotating ? SolarFarmState.Rotating : worker);
            }

            private static IEnumerable<SolarFarmArm> CreateArms(IEnumerable<IMyMotorStator> stators, IEnumerable<IMySolarPanel> solarPanels, double minAngle, double maxAngle)
            {
                return stators.Select(stator =>
                {
                    var getGroupRegex = new Regex($"(?<={NameLike} ).*?(?=])");
                    var group = getGroupRegex.Match(stator.Name);
                    var groupTagRegex = new Regex($"({NameLike} {group}\\])");
                    var filteredSolarPanels = solarPanels.Where(solarPanel => groupTagRegex.IsMatch(solarPanel.Name));
                    return new SolarFarmArm(stator, filteredSolarPanels, minAngle, maxAngle);
                });
            }
        }

        private class SolarFarmArm
        {
            private readonly double _minAngle;
            private readonly double _maxAngle;
            private readonly IMyMotorStator _stator;
            private readonly IEnumerable<IMySolarPanel> _solarPanels;
            private double _powerProduction;
            private int _inferredSunDirection;
            private List<int> _previousDirections;

            public enum SolarFarmArmState
            {
                Ready,
                Rotating
            }

            public SolarFarmArm(IMyMotorStator stator, IEnumerable<IMySolarPanel> solarPanels, double minAngle, double maxAngle)
            {
                _stator = stator;
                _solarPanels = solarPanels;
                _minAngle = minAngle;
                _maxAngle = maxAngle;
                _previousDirections = new List<int>();
                _powerProduction = double.NegativeInfinity;
            }

            public SolarFarmArmState Run()
            {
                var newPowerProduction = GetCurrentPowerProduction();

                if (newPowerProduction < _powerProduction) {
                    _previousDirections.Add(_inferredSunDirection);
                    if (IsInLoop())
                    {
                        return SolarFarmArmState.Ready;
                    }
                    _inferredSunDirection = _inferredSunDirection * -1;
                }

                _previousDirections.Add(_inferredSunDirection);
                _powerProduction = newPowerProduction;

                double statorPlusSun = _stator.Angle + _inferredSunDirection;
                var newAngle = statorPlusSun > _maxAngle ? _minAngle : statorPlusSun < _minAngle ? _maxAngle : statorPlusSun;

                return RotateStatorToAngle(_stator, newAngle) ? SolarFarmArmState.Ready : SolarFarmArmState.Rotating;
            }

            private bool IsInLoop()
            {
                var count = _previousDirections.Count;
                if (count <= 2) return false;
                return _previousDirections[count - 1] == _previousDirections[count - 3];
            }

            private double GetCurrentPowerProduction()
            {
                return _solarPanels.Aggregate(0.0, (worker, next) => worker + next.CurrentOutput);
            }

            private static bool RotateStatorToAngle(IMyMotorStator stator, double angle)
            {
                if (IsAngleCloseEnough(angle, RadiansToDegrees(stator.Angle)))
                {
                    stator.BrakingTorque = StatorBreakingTorque;
                    stator.TargetVelocityRPM = 0;
                    stator.BrakingTorque = StatorBreakingTorque;
                    return true;
                }
                stator.Torque = StatorTorque;
                stator.TargetVelocityRPM = stator.Angle > angle ? -StatorSlowRpm : StatorSlowRpm;
                return false;
            }

            private static bool IsAngleCloseEnough(double aimAngle, double currentAngle)
            {
                var angleDiff = Math.Abs(aimAngle - currentAngle);
                return angleDiff <= AnglePrecision || angleDiff >= (360 - AnglePrecision);
            }

            /*private double DegreesToRadians(double degrees)
            {
                return (Math.PI / 180) * degrees;
            }*/

            private static double RadiansToDegrees(double radians)
            {
                return (180 / Math.PI) * radians;
            }
        }

        private class Logger{
            private readonly List<IMyTextPanel> _panels;
        
            public Logger(List<IMyTextPanel> panels){
                _panels = panels;
                Clear();
            }

            public void Log(string message){
                foreach(var lcd in _panels){
                    lcd.WritePublicText(message + "\n", true);
                }
            }

            public void Clear()
            {
                foreach (var panel in _panels)
                {
                    panel.WritePublicText("");
                }
            }
        }
#region post-cript
    }
}
#endregion