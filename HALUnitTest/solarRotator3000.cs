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
        private const UpdateFrequency IdleUpdateFrequency = UpdateFrequency.Update10;
        private const UpdateFrequency WorkingUpdateFrequency = UpdateFrequency.Update1;
        private const int PlanetInferFrequency = 0;
        private const int PlanetInitialInferredSunDirection = 1;
        private readonly SolarFarm _solarFarm;
        private readonly Logger _logger;

        public Program()
        {
            Runtime.UpdateFrequency = IdleUpdateFrequency;

            var stators = FindBlocksOfType<IMyMotorStator>(NameLike);
            var solarPanels = FindBlocksOfType<IMySolarPanel>(NameLike);
            var lcdPanels = FindBlocksOfType<IMyTextPanel>(NameLike);

            _solarFarm = new SolarFarm(stators, solarPanels, PlanetInferFrequency, PlanetInitialInferredSunDirection, PlanetMinAngle, PlanetMaxAngle);
            _logger = new Logger(lcdPanels);
        }
        
        public void Main()
        {
            var solarFarmState = _solarFarm.Run();
            _logger.Clear();
            _logger.Log($"Solar Farm State: {solarFarmState}");
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
            private readonly IEnumerable<SolarFarmArm> _solarFarmArms;

            public enum SolarFarmState
            {
                Idle,
                Working
            }

            public SolarFarm(IEnumerable<IMyMotorStator> stators, IEnumerable<IMySolarPanel> solarPanels, int inferFrequency, int initialInferredSunDirection, double minAngle, double maxAngle)
            {
                _solarFarmArms = CreateArms(stators, solarPanels, inferFrequency, initialInferredSunDirection, minAngle, maxAngle);
            }

            public SolarFarmState Run()
            {
                // If any solar farm arm is not idle the whole solar farm is set as working
                return _solarFarmArms.Any(s => s.Run() != SolarFarmArm.SolarFarmArmState.Idle) ? SolarFarmState.Working : SolarFarmState.Idle;
            }

            private static IEnumerable<SolarFarmArm> CreateArms(IEnumerable<IMyMotorStator> stators, IEnumerable<IMySolarPanel> solarPanels, int inferFrequency, int initialInferredSunDirection, double minAngle, double maxAngle)
            {
                var getGroupRegex = new Regex($"(?<={NameLike} ).*?(?=])");
                return stators.Select(stator =>
                {
                    var group = getGroupRegex.Match(stator.Name);
                    var groupTagRegex = new Regex($"({NameLike} {group}\\])");
                    var filteredSolarPanels = solarPanels.Where(solarPanel => groupTagRegex.IsMatch(solarPanel.Name));
                    return new SolarFarmArm(stator, filteredSolarPanels, inferFrequency, initialInferredSunDirection, minAngle, maxAngle);
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
            private readonly List<int> _previousDirections;
            private readonly int _inferFrequency;
            private double _currentAimAngle;

            private SolarFarmArmState _state;

            public enum SolarFarmArmState
            {
                Idle,
                Rotating,
                Inferring
            }

            public SolarFarmArm(IMyMotorStator stator, IEnumerable<IMySolarPanel> solarPanels, int inferFrequency, int initialInferredSunDirection, double minAngle, double maxAngle)
            {
                _stator = stator;
                _solarPanels = solarPanels;
                _minAngle = minAngle;
                _maxAngle = maxAngle;
                _previousDirections = new List<int>();
                _powerProduction = double.NegativeInfinity;
                _inferFrequency = inferFrequency;
                _state = SolarFarmArmState.Idle;
                _inferredSunDirection = initialInferredSunDirection;
            }

            public SolarFarmArmState Run()
            {
                var oldPowerProduction = _powerProduction;
                _powerProduction = GetCurrentPowerProduction();

                switch (_state)
                {
                    case SolarFarmArmState.Idle:
                        if (oldPowerProduction > _powerProduction) // Power production no longer optimal, starting rotation
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
                        if (_powerProduction < oldPowerProduction) // Optimal power production reached, stopping rotation
                        {
                            StopStator(_stator);
                            if (ShouldInferSunDirection())
                            {
                                _currentAimAngle = _stator.Angle + -1 * _inferredSunDirection;
                                RotateStatorToAngle(_stator, _currentAimAngle);
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
                        throw new ArgumentOutOfRangeException($"New unhandled state with name \"{_state}\" was added to SolarFarmArmState");
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

            private static bool RotateStatorToAngle(IMyMotorStator stator, double angle)
            {
                if (IsAngleCloseEnough(angle, RadiansToDegrees(stator.Angle)))
                {
                    StopStator(stator);
                    return true;
                }
                stator.Torque = StatorTorque;
                stator.TargetVelocityRPM = stator.Angle > angle ? -StatorSlowRpm : StatorSlowRpm;
                return false;
            }

            private static void StopStator(IMyMotorStator stator)
            {
                stator.BrakingTorque = StatorBreakingTorque;
                stator.TargetVelocityRPM = 0;
                stator.BrakingTorque = StatorBreakingTorque;
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