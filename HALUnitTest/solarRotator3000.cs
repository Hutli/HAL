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
        private const int PlanetInferFrequency = 0;
        private const int PlanetInitialInferredSunDirection = 1;
        private const string initiationStringArgument = "Initiate";
        private readonly SolarFarm _solarFarm;
        private readonly Logger _logger;
        private double _overrideAngle;

        public Program()
        {
            Runtime.UpdateFrequency = IdleUpdateFrequency;

            var stators = FindBlocksOfType<IMyMotorStator>(NameLike);
            var solarPanels = FindBlocksOfType<IMySolarPanel>(NameLike);
            var lcdPanels = FindBlocksOfType<IMyTextPanel>(NameLike);

            _logger = new Logger(lcdPanels);

            _solarFarm = new SolarFarm(stators, solarPanels, PlanetInferFrequency, PlanetInitialInferredSunDirection, PlanetMinAngle, PlanetMaxAngle, _logger);
        }
        
        public void Main(string argument)
        {
            double angle;
            if (double.TryParse(argument, out angle))
            {
                _overrideAngle = angle;
            } else if (argument == initiationStringArgument)
            {
                _overrideAngle = double.NaN;
            }

            _logger.Clear();
            _logger.Log($"Override Angle: {_overrideAngle}");
            _logger.Log($"Number of Salor Farm Arms: {_solarFarm.CountArms()}");
            var solarFarmState = _solarFarm.Run(_overrideAngle);

            switch (solarFarmState)
            {
                case SolarFarm.SolarFarmState.Idle:
                    Runtime.UpdateFrequency = IdleUpdateFrequency;
                    break;
                case SolarFarm.SolarFarmState.Working:
                    Runtime.UpdateFrequency = WorkingUpdateFrequency;
                    break;
                default:
                    Runtime.UpdateFrequency = ErrorUpdateFrequency;
                    _logger.Log("Unknown state, terminating execution");
                    break;
            }
        }

        private List<T> FindBlocksOfType<T>(string nameLike) where T : class
        {
            var blocks = new List<T>();

            GridTerminalSystem.GetBlocksOfType(blocks);

            return blocks.Where(x => ((IMyTerminalBlock)x).CustomName.Contains(nameLike)).ToList();
        }

        public class SolarFarm
        {
            private readonly Logger _logger;
            private readonly IEnumerable<SolarFarmArm> _solarFarmArms;

            public int CountArms()
            {
                return _solarFarmArms.Count();
            }

            public enum SolarFarmState
            {
                Idle,
                Working
            }

            public SolarFarm(IEnumerable<IMyMotorStator> stators, IEnumerable<IMySolarPanel> solarPanels, int inferFrequency, int initialInferredSunDirection, double minAngle, double maxAngle, Logger logger)
            {
                _logger = logger;
                _solarFarmArms = CreateArms(stators, solarPanels, inferFrequency, initialInferredSunDirection, minAngle, maxAngle, logger);
                _logger.Log("Solar farm created");
            }

            public SolarFarmState Run(double overrideAngle)
            {
                // If any solar farm arm is not idle the whole solar farm is set as working
                var states = _solarFarmArms.Select(s => s.Run(overrideAngle)).ToList();
                return states.Any(s => s != SolarFarmArm.SolarFarmArmState.Idle) ? SolarFarmState.Working : SolarFarmState.Idle;
            }

            private IEnumerable<SolarFarmArm> CreateArms(IEnumerable<IMyMotorStator> stators, IEnumerable<IMySolarPanel> solarPanels, int inferFrequency, int initialInferredSunDirection, double minAngle, double maxAngle, Logger logger)
            {
                var getGroupRegex = new System.Text.RegularExpressions.Regex($"(?<=\\{NameLike} ).*?(?=])");
                return stators.Select(stator =>
                {
                    var group = getGroupRegex.Match(stator.CustomName);
                    var groupTagRegex = new System.Text.RegularExpressions.Regex($"(\\{NameLike} {group}\\])");
                    var filteredSolarPanels = solarPanels.Where(solarPanel => groupTagRegex.IsMatch(solarPanel.CustomName));
                    return new SolarFarmArm(group.ToString(), stator, filteredSolarPanels, inferFrequency, initialInferredSunDirection, minAngle, maxAngle, logger);
                }).Where(stator => stator != null).ToList();
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
            private readonly List<int> _previousDirections;
            private readonly int _inferFrequency;
            private double _currentAimAngle;
            private Logger _logger;
            private SolarFarmArmState _state;
            private double _overrideAngle;

            public string Name { get; }

            public enum SolarFarmArmState
            {
                Idle,
                Rotating,
                Inferring
            }

            public SolarFarmArm(string groupName, IMyMotorStator stator, IEnumerable<IMySolarPanel> solarPanels, int inferFrequency, int initialInferredSunDirection, double minAngle, double maxAngle, Logger logger)
            {
                Name = groupName;
                _stator = stator;
                _solarPanels = solarPanels;
                _minAngle = minAngle;
                _maxAngle = maxAngle;
                _previousDirections = new List<int>();
                _powerProduction = double.NegativeInfinity;
                _inferFrequency = inferFrequency;
                _state = SolarFarmArmState.Idle;
                _inferredSunDirection = initialInferredSunDirection;
                _logger = logger;
            }

            public SolarFarmArmState Run(double overrideAngle)
            {
                _overrideAngle = overrideAngle;
                var oldPowerProduction = _powerProduction;
                _powerProduction = GetCurrentPowerProduction();
                var currentAngle = RadiansToDegrees(_stator.Angle);

                _logger.Log($"Solar farm arm {Name} with state {_state}");
                _logger.Log($"Override angle: {_overrideAngle} | Current angle: {currentAngle}");
                _logger.Log($"Old pow: {string.Format("{0:N4}", oldPowerProduction)} | New pow {string.Format("{0:N4}", _powerProduction)}");
                _logger.Log($"Solar panels: {_solarPanels.Count()}");

                switch (_state)
                {
                    case SolarFarmArmState.Idle:
                        if (!double.IsNaN(_overrideAngle) && !IsAngleCloseEnough(_overrideAngle, currentAngle) || // Angle overridden skipping normal oprerations
                            double.IsNaN(_overrideAngle) && oldPowerProduction > _powerProduction) // Power production no longer optimal, starting rotation
                        {
                            _state = SolarFarmArmState.Rotating;
                        }
                        break;

                    case SolarFarmArmState.Inferring:
                        _inferredSunDirection = InferSunDirection();
                        _previousDirections.Add(_inferredSunDirection);
                        _state = SolarFarmArmState.Idle;
                        break;

                    case SolarFarmArmState.Rotating:
                        if (!double.IsNaN(_overrideAngle)) // Angle overridden skipping normal oprerations
                        {
                            _logger.Log($"Angle overridden rotating to {_overrideAngle}");
                            if (RotateStatorToAngle(_stator, _overrideAngle))
                            {
                                _state = SolarFarmArmState.Idle;
                            }
                        }
                        else if (_powerProduction < oldPowerProduction) // Optimal power production reached, stopping rotation
                        {
                            StopStator(_stator);
                            if (ShouldInferSunDirection())
                            {
                                _currentAimAngle = _stator.Angle + -1 * _inferredSunDirection;
                                _state = SolarFarmArmState.Inferring;
                            }
                            _state = SolarFarmArmState.Idle;
                        }
                        else // Power production still suppar, continue rotating
                        {
                            if (RotateStatorToAngle(_stator, _currentAimAngle)) // Aim angle reached, setting new aim angle
                            {
                                double statorPlusSun = _stator.Angle + _inferredSunDirection;
                                _currentAimAngle = statorPlusSun > _maxAngle
                                    ? _minAngle
                                    : statorPlusSun < _minAngle
                                        ? _maxAngle
                                        : statorPlusSun;
                            }
                        }
                        break;
                    default:
                        throw new Exception($"New unhandled state with name \"{_state}\" was added to SolarFarmArmState");
                }
                return _state;
            }

            private int InferSunDirection()
            {
                if (IsInLoop())
                {
                    return _inferredSunDirection;
                }
                return _inferredSunDirection = _inferredSunDirection * -1;
            }

            private bool ShouldInferSunDirection()
            {
                return _inferFrequency > 0;
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

            private bool RotateStatorToAngle(IMyMotorStator stator, double angle)
            {
                var currentAngle = RadiansToDegrees(stator.Angle);
                if (IsAngleCloseEnough(angle, currentAngle))
                {
                    StopStator(stator);
                    return true;
                }
                stator.Torque = StatorTorque;
                stator.TargetVelocityRPM = currentAngle - angle < 180 && currentAngle - angle > 0 ? -StatorSlowRpm : StatorSlowRpm;
                return false;
            }

            private static void StopStator(IMyMotorStator stator)
            {
                stator.BrakingTorque = StatorBreakingTorque;
                stator.TargetVelocityRPM = 0;
                stator.BrakingTorque = StatorBreakingTorque;
            }

            private bool IsAngleCloseEnough(double aimAngle, double currentAngle)
            {
                var angleDiff = Math.Abs(aimAngle - currentAngle);
                _logger.Log($"Angle diff: {angleDiff}");
                return angleDiff <= AnglePrecision;
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

        public class Logger{
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