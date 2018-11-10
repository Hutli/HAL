#region pre-script
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Linq;
using VRageMath;
using System.Text.RegularExpressions;

namespace IngameScript {
    public class Program : MyGridProgram {
        public void SetGridTerminalSystem (IMyGridTerminalSystem gridTerminalSystem) {
            GridTerminalSystem = gridTerminalSystem;
        }

        #endregion
        //To put your code in a PB copy from this comment...

        private struct NameLike {
            public string nameLike;
            public Func<string, IMyGridTerminalSystem, AccessLogicGroup> createObject;

            public NameLike(string inputNameLike, Func<string, IMyGridTerminalSystem, AccessLogicGroup> inputCreateObject) {
                nameLike = inputNameLike;
                createObject = inputCreateObject;
            }

            public override string ToString() {
                return nameLike;
            }
        }

        private static readonly List<NameLike> typeTranslations = new List<NameLike> { new NameLike("AL", (x, y) => new Airlock(x, y)),
                                                                                       new NameLike("RC", (x, y) => new RotationCockpit(x, y))};
        private static readonly List<string> delimitors = new List<string> { "I", "O" };

        private List<AccessLogicGroup> accessLogicGroups = new List<AccessLogicGroup>();
        private List<IMyProgrammableBlock> hals = new List<IMyProgrammableBlock>();

        public Program() {
            //accessLogicGroups.Add(new RotationCockpit("[HAL RC1", GridTerminalSystem));
            //accessLogicGroups.Add(new Airlock("[HAL AL1", GridTerminalSystem));
        }

        public void Save() {
            // Called when the program needs to save its state. Use     
            // this method to save your state to the Storage field     
            // or some other means.      
            //      
            // This method is optional and can be removed if not     
            // needed.  
        }

        public void Main(string argument) {
            UpdateLists();
            RunAccessLogic(argument);
            //PrintStates();
        }

        /*private void PrintStates() {
            AccessLogicGroup helper = new AccessLogicGroup("", GridTerminalSystem);
            List<IMyTextPanel> panels = helper.FindBlocksOfType<IMyTextPanel>("[HAL");

            panels.ForEach(x => { x.WritePublicText(((Airlock)accessLogicGroups[1]).ToString()); x.ShowPublicTextOnScreen(); });
        }*/

        private void RunAccessLogic(string argument) {
            bool autoRunScript = false;
            accessLogicGroups.ForEach(accessLogicGroup => {
                accessLogicGroup.UpdateBlocks();
                accessLogicGroup.UpdateAccessLogic(argument);
                autoRunScript |= accessLogicGroup.IsRunning;
            });
            hals.ForEach(x => x.CustomName = autoRunScript ? "Program [HAL] {LoopComputers:0.016}" : "Program [HAL]");
        }

        private void UpdateLists() {
            AccessLogicGroup helper = new AccessLogicGroup("", GridTerminalSystem);
            
            List<NameLike> currentNameLikes = GetAllGroups();
            List<NameLike> newNameLikes = currentNameLikes.FindAll(x => !accessLogicGroups.Exists(y => y.NameLike == x.nameLike));

            // Add all newly created access logic groups
            newNameLikes.ForEach(x => accessLogicGroups.Add(x.createObject(x.nameLike, GridTerminalSystem)));
            
            // Remove all access logic groups has been deleted
            accessLogicGroups.RemoveAll(x => !currentNameLikes.Exists(y => y.nameLike == x.NameLike));

            // The programmable blocks does not have a state that needs to be kept, so they are simply all replaced
            hals = helper.FindBlocksOfType<IMyProgrammableBlock>("[HAL]");
        }

        private List<NameLike> GetAllGroups() {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            List<NameLike> returnNameLikes = new List<NameLike>();
            GridTerminalSystem.SearchBlocksOfName("[HAL ", blocks);

            blocks.ForEach(block => {
                string joinedStrings = string.Join("|", typeTranslations.Select(x => x.nameLike));
                typeTranslations.ForEach(translation => {
                    System.Text.RegularExpressions.Match tmpMatch = System.Text.RegularExpressions.Regex.Match(block.CustomName, @"\[HAL (" + translation.nameLike + @")\d+(\:" + string.Join("|", delimitors) + @")?\]");
                    if(tmpMatch.Success) {
                        string tmpNameLike = System.Text.RegularExpressions.Regex.Match(tmpMatch.Value, @"\[HAL " + translation.nameLike + @"\d+").Value;
                        returnNameLikes.Add(new NameLike(tmpNameLike, translation.createObject));
                    }
                });
            });
            return returnNameLikes.Distinct().ToList();
        }

        private class AccessLogicGroup {
            protected IMyGridTerminalSystem _gridTerminalSystem;
            protected bool _isRunning = false;
            protected string _nameLike = "";
            protected string _changeState { get { return _nameLike + "] Change"; } }

            public string NameLike { get { return _nameLike; } }

            public bool IsRunning { get { return _isRunning; } }
            
            public AccessLogicGroup(string nameLike, IMyGridTerminalSystem gridTerminalSystem) {
                _nameLike = nameLike;
                _gridTerminalSystem = gridTerminalSystem;
            }

            public virtual void UpdateAccessLogic(string argument) { }
            public virtual void UpdateBlocks() { }

            public List<T> FindBlocksOfType<T>(string nameLike) where T : class {

                List<T> blocks = new List<T>();
                _gridTerminalSystem.GetBlocksOfType<T>(blocks);

                return blocks.Where(x => ((IMyTerminalBlock)x).CustomName.Contains(nameLike)).ToList();
            }

            protected bool AreDoorsOpen(List<IMyDoor> doors) {
                return doors.TrueForAll(x => x.Status == DoorStatus.Open);
            }

            protected bool AreDoorsClosed(List<IMyDoor> doors) {
                return doors.TrueForAll(x => x.Status == DoorStatus.Closed);
            }

            protected bool AreDoorsLocked(List<IMyDoor> doors) {
                return doors.TrueForAll(x => !x.Enabled);
            }

            protected bool AreDoorsUnlocked(List<IMyDoor> doors) {
                return doors.TrueForAll(x => x.Enabled);
            }

            protected void LockDoors(List<IMyDoor> doors) {
                doors.ForEach(x => x.Enabled = false);
            }

            protected void UnlockDoors(List<IMyDoor> doors) {
                doors.ForEach(x => x.Enabled = true);
            }

            protected void OpenDoors(List<IMyDoor> doors) {
                doors.ForEach(x => x.OpenDoor());
            }

            protected void CloseDoors(List<IMyDoor> doors) {
                doors.ForEach(x => x.CloseDoor());
            }
        }

        private class RotationCockpit : AccessLogicGroup {
            const int ANGLE_PRECISION = 2;

            private enum RotationState {
                Rotating,
                Locked
            };

            private enum PistonState {
                In,
                Out,
            }

            private struct RotationCockpitGridBlocks {
                public List<IMyMotorStator> rotor;
                public List<IMyPistonBase> pistons;
            };

            private RotationState _rotationState = RotationState.Locked;
            private PistonState _pistonState = PistonState.In;

            private string _rotation { get { return _nameLike + "]"; } }

            private int _currentAimAngle = 0;

            private Airlock _airlock;

            private List<IMyPistonBase> _pistons = new List<IMyPistonBase>();
            private List<IMyMotorStator> _rotors = new List<IMyMotorStator>();

            public RotationCockpit(string nameLike, IMyGridTerminalSystem gridTerminalSystem) : base(nameLike, gridTerminalSystem) {
                _airlock = new Airlock(nameLike, gridTerminalSystem);

                _pistonState = PistonState.In;
                _rotationState = RotationState.Rotating;

                UpdateBlocks();
                UpdateAccessLogic("");
            }

            override public void UpdateBlocks() {
                RotationCockpitGridBlocks newBlocks = FindRotationCockpitBlocksInGrid(_nameLike);
                _airlock.UpdateBlocks();

                _pistons = newBlocks.pistons;
                _rotors = newBlocks.rotor;
            }

            override public void UpdateAccessLogic(string argument) {
                _airlock.UpdateAccessLogic("");
                int tmpArgumentAngle = GetArgumentAngle(argument);

                if(argument == _changeState) {
                    ChangeState();
                } else if(tmpArgumentAngle >= 0) {
                    _currentAimAngle = tmpArgumentAngle;
                    _rotationState = RotationState.Rotating;
                }

                _isRunning = true;

                switch(_pistonState) {
                    case PistonState.In:
                        if(_rotationState == RotationState.Locked) {
                            if(ArePistonsCompressed(_pistons)) {
                                LockPistons(_pistons);
                                if(_airlock.State == Airlock.AirlockState.In) {
                                    if(!_airlock.IsRunning) {
                                        _isRunning = false;
                                    }
                                } else {
                                    _airlock.UpdateAccessLogic(_changeState);
                                }
                            } else {
                                UnlockPistons(_pistons);
                                Compress(_pistons);
                            }
                        }
                        break;

                    case PistonState.Out:
                        if(_airlock.State == Airlock.AirlockState.Out) {
                            if(!_airlock.IsRunning) {
                                if(_rotationState == RotationState.Locked) {
                                    if(ArePistonsExtended(_pistons)) {
                                        LockPistons(_pistons);
                                        _isRunning = false;
                                    } else {
                                        UnlockPistons(_pistons);
                                        Extend(_pistons);
                                    }
                                }
                            }
                        } else {
                            _airlock.UpdateAccessLogic(_changeState);
                        }
                        break;
                }

                switch(_rotationState) {
                    case RotationState.Locked:
                        if(IsAngleCloseEnough(_currentAimAngle, GetCurrentAngle(_rotors))) {
                            LockRotors(_rotors);
                        } else {
                            _rotationState = RotationState.Rotating;
                        }
                        break;

                    case RotationState.Rotating:
                        if(ArePistonsExtended(_pistons) || _currentAimAngle == 0) {
                            if(IsAngleCloseEnough(_currentAimAngle, GetCurrentAngle(_rotors))) {
                                LockRotors(_rotors);
                                _rotationState = RotationState.Locked;
                            } else {
                                UnlockRotors(_rotors);
                                RotateToPosition(_currentAimAngle, _rotors);
                            }
                        } else {
                            _currentAimAngle = 0;
                        }
                        break;
                }
            }

            public override string ToString() {
                return
                    "RC running: " + _isRunning
                    + "\nAL running: " + _airlock.IsRunning
                    + "\nPiston state: " + _pistonState
                    + "\nRotor state: " + _rotationState
                    + "\nAirlock state: " + _airlock.State;
            }

            private RotationCockpitGridBlocks FindRotationCockpitBlocksInGrid(string nameLike) { // Warning: Function not general, should be changed   
                RotationCockpitGridBlocks rotationCockpitGridBlocks = new RotationCockpitGridBlocks();

                rotationCockpitGridBlocks.rotor = FindBlocksOfType<IMyMotorStator>(nameLike + "]");
                rotationCockpitGridBlocks.pistons = FindBlocksOfType<IMyPistonBase>(nameLike + "]");

                return rotationCockpitGridBlocks;
            }

            private void ChangeState() {
                _currentAimAngle = 0;
                _rotationState = RotationState.Rotating;
                if(_pistonState == PistonState.In) {
                    _pistonState = PistonState.Out;
                } else {
                    _pistonState = PistonState.In;
                }
                _isRunning = true;
            }

            private void RotateToPosition(int aimAngle, List<IMyMotorStator> rotors) {
                rotors.ForEach(rotor => {
                    rotor.Torque = 100000;

                    int angleDiff = aimAngle - GetCurrentAngle(rotor);

                    if(IsAngleCloseEnough(aimAngle, GetCurrentAngle(rotor))) {
                        // Do not rotate     
                    } else if(angleDiff > 0 && angleDiff <= 180 || angleDiff < -180) {
                        // TODO svs: TargetVelocity was 20. Use same unit?
                        rotor.TargetVelocityRad = 20;
                    } else if(angleDiff > 180 || angleDiff < 0 && angleDiff >= -180) {
                        // TODO svs: TargetVelocity was -20. Use same unit?
                        rotor.TargetVelocityRad = -20;
                    }
                });
            }

            private void LockPistons(List<IMyPistonBase> pistons) {
                pistons.ForEach(x => x.Enabled = false);
            }

            private void UnlockPistons(List<IMyPistonBase> pistons) {
                pistons.ForEach(x => x.Enabled = true);
            }

            private void LockRotors(List<IMyMotorStator> rotors) {
                rotors.ForEach(rotor => {
                    rotor.Enabled = true;
                    rotor.BrakingTorque = 448000;
                    rotor.TargetVelocityRad = 0;
                    rotor.Enabled = false;
                });
            }

            private void UnlockRotors(List<IMyMotorStator> rotors) {
                rotors.ForEach(rotor => {
                    rotor.Enabled = true;
                    rotor.TargetVelocityRad = 0;
                });
            }

            private bool IsAngleCloseEnough(int aimAngle, int currentAngle) {
                int angleDiff = Math.Abs(aimAngle - currentAngle);
                return angleDiff <= ANGLE_PRECISION || angleDiff >= 360 - ANGLE_PRECISION;
            }

            private bool ArePistonsExtended(List<IMyPistonBase> pistons) {
                return pistons.TrueForAll(x => (GetCurrentPosition(x) >= x.MaxLimit));
            }

            private bool ArePistonsCompressed(List<IMyPistonBase> pistons) {
                return pistons.TrueForAll(x => (GetCurrentPosition(x) <= x.MinLimit));
            }

            private int GetArgumentAngle(string argument) {
                int tmpAngle = -1;
                int tmpIndex = argument.IndexOf(_nameLike);
                if(tmpIndex >= 0) {
                    tmpIndex += _nameLike.Count() + 1;
                    if(!Int32.TryParse(argument.Substring(tmpIndex), out tmpAngle)) {
                        tmpAngle = -1;
                    }
                }
                return tmpAngle;
            }

            private int GetCurrentAngle(List<IMyMotorStator> rotors) {
                return rotors.Aggregate<IMyMotorStator, int>(0, (aggregatedAngle, rotor) => {
                    string tmpString = rotor.DetailedInfo.Substring(rotor.DetailedInfo.IndexOf("Current angle") + 14);
                    return aggregatedAngle + Convert.ToInt32(tmpString.Remove(tmpString.Length - 1));
                });
            }

            private int GetCurrentAngle(IMyMotorStator rotor) {
                string tmpString = rotor.DetailedInfo.Substring(rotor.DetailedInfo.IndexOf("Current angle") + 14);
                return Convert.ToInt32(tmpString.Remove(tmpString.Length - 1));
            }

            private double GetCurrentPosition(IMyPistonBase piston) {
                return piston.CurrentPosition;
                /*string tmpString = piston.DetailedInfo.Substring(piston.DetailedInfo.IndexOf("Current position") + 18);
                return Convert.ToDouble(tmpString.Remove(tmpString.Length - 1));*/
            }

            private void Extend(List<IMyPistonBase> pistons) {
                pistons.ForEach(x => x.Velocity = 2);
            }

            private void Compress(List<IMyPistonBase> pistons) {
                pistons.ForEach(x => x.Velocity = -2);
            }
        }

        private class Airlock : AccessLogicGroup {
            public enum AirlockState {
                In,
                Out
            }

            private struct AirlockGridBlocks {
                public List<IMyDoor> outerDoors;
                public List<IMyDoor> innerDoors;
                public List<IMyLightingBlock> lights;
                public List<IMyAirVent> vents;
                public List<IMySoundBlock> innerSpeakers;
                public List<IMySoundBlock> outerSpeakers;
                public List<IMyGasTank> gasTanks;
            };

            private AirlockState _airlockState = AirlockState.In;

            private List<IMyDoor> _outerDoors = new List<IMyDoor>();
            private List<IMyDoor> _innerDoors = new List<IMyDoor>();
            private List<IMyLightingBlock> _lights = new List<IMyLightingBlock>();
            private List<IMyAirVent> _vents = new List<IMyAirVent>();
            private List<IMySoundBlock> _innerSpeakers = new List<IMySoundBlock>();
            private List<IMySoundBlock> _outerSpeakers = new List<IMySoundBlock>();
            private List<IMyGasTank> _gasTanks = new List<IMyGasTank>();
            
            public AirlockState State {
                get { return _airlockState; }
            }

            public Airlock(string nameLike, IMyGridTerminalSystem gridTerminalSystem) : base(nameLike, gridTerminalSystem) {
                _airlockState = AirlockState.In;
                UpdateBlocks();
                UpdateAccessLogic("");
            }

            override public void UpdateBlocks() {
                AirlockGridBlocks newBlocks = FindAirlockBlocksInGrid(_nameLike);
                _lights = newBlocks.lights;
                _innerDoors = newBlocks.innerDoors;
                _outerDoors = newBlocks.outerDoors;
                _vents = newBlocks.vents;
                _innerSpeakers = newBlocks.innerSpeakers;
                _outerSpeakers = newBlocks.outerSpeakers;
                _gasTanks = newBlocks.gasTanks;
            }

            override public void UpdateAccessLogic(string argument) {
                if(argument == _changeState) {
                    if(_airlockState == AirlockState.In) {
                        PlaySound(_outerSpeakers);
                        _airlockState = AirlockState.Out;
                    } else {
                        PlaySound(_innerSpeakers);
                        _airlockState = AirlockState.In;
                    }
                    BlinkLights(_lights);
                }

                _isRunning = true;

                switch(_airlockState) {
                    case AirlockState.In:
                        if(AreDoorsClosed(_outerDoors)) {
                            LockDoors(_outerDoors);
                            if(Pressurize(_vents, _gasTanks)) {
                                if(AreDoorsOpen(_innerDoors)) {
                                    LockDoors(_innerDoors);
                                    ReadyLights(_lights);
                                    _isRunning = false;
                                } else {
                                    UnlockDoors(_innerDoors);
                                    OpenDoors(_innerDoors);
                                }
                            }
                        } else {
                            UnlockDoors(_outerDoors);
                            CloseDoors(_outerDoors);
                        }
                        break;

                    case AirlockState.Out:
                        if(AreDoorsClosed(_innerDoors)) {
                            LockDoors(_innerDoors);
                            if(Depressurize(_vents, _gasTanks)) {
                                if(AreDoorsOpen(_outerDoors)) {
                                    LockDoors(_outerDoors);
                                    ReadyLights(_lights);
                                    _isRunning = false;
                                } else {
                                    UnlockDoors(_outerDoors);
                                    OpenDoors(_outerDoors);
                                }
                            }
                        } else {
                            UnlockDoors(_innerDoors);
                            CloseDoors(_innerDoors);
                        }
                        break;
                }
            }

            private void PlaySound(List<IMySoundBlock> speakers) {
                speakers.ForEach(x => x.Play());
            }

            public override string ToString() {
                return "Oxygen in room: " + _vents[0].GetOxygenLevel() * 100 + "%\nOxygen in tanks: " + _gasTanks[0].FilledRatio * 100 + "%\nState: " + _airlockState;
                //"Outer doors: " + _outerDoors.Count + "\nInner doors: " + _innerDoors.Count + "\nLights: " + _lights.Count + "\nVents: " + _vents.Count;
            }

            private AirlockGridBlocks FindAirlockBlocksInGrid(string nameLike) { // Warning: Function not general, should be changed   
                AirlockGridBlocks airlockGridBlocks = new AirlockGridBlocks();

                airlockGridBlocks.lights = FindBlocksOfType<IMyLightingBlock>(nameLike + "]");
                airlockGridBlocks.innerDoors = FindBlocksOfType<IMyDoor>(nameLike + ":I]");
                airlockGridBlocks.outerDoors = FindBlocksOfType<IMyDoor>(nameLike + ":O]");
                airlockGridBlocks.vents = FindBlocksOfType<IMyAirVent>(nameLike + "]");
                airlockGridBlocks.innerSpeakers = FindBlocksOfType<IMySoundBlock>(nameLike + ":I]");
                airlockGridBlocks.outerSpeakers = FindBlocksOfType<IMySoundBlock>(nameLike + ":O]");
                airlockGridBlocks.gasTanks = FindBlocksOfType<IMyGasTank>(nameLike + "]");
                

                return airlockGridBlocks;
            }

            private bool AreTanksFull(List<IMyGasTank> gasTanks) {
                return gasTanks.TrueForAll(x => x.FilledRatio >= 1);
            }

            private bool AreTanksEmpty(List<IMyGasTank> gasTanks) {
                return gasTanks.TrueForAll(x => x.FilledRatio <= 0);
            }

            private bool AreVentsDepressurized(List<IMyAirVent> vents) {
                return vents.TrueForAll(x => x.GetOxygenLevel() <= 0);
            }

            private bool AreVentsPressurized(List<IMyAirVent> vents) {
                return vents.TrueForAll(x => x.GetOxygenLevel() >= 1);
            }

            private bool Depressurize(List<IMyAirVent> vents, List<IMyGasTank> gasTanks) {
                bool returnBool = true;

                if(!AreTanksFull(gasTanks)) {
                    vents.ForEach(x => {
                        if(x.GetOxygenLevel() > 0) {
                            x.Enabled = true;
                            x.Depressurize = true;
                            returnBool = false;
                        } else {
                            x.Depressurize = false;
                            x.Enabled = false;
                        }
                    });
                }

                return returnBool;
            }

            private bool Pressurize(List<IMyAirVent> vents, List<IMyGasTank> gasTanks) {
                bool returnBool = true;

                if(!AreTanksEmpty(gasTanks)) {
                    vents.ForEach(x => {
                        if(x.GetOxygenLevel() < 1) {
                            x.Enabled = true;
                            x.Depressurize = false;
                            returnBool = false;
                        } else {
                            x.Enabled = false;
                        }
                    });
                }

                return returnBool;
            }

            private void BlinkLights(List<IMyLightingBlock> lights) {
                lights.ForEach(x => {
                    x.Enabled = true;
                    x.BlinkIntervalSeconds = 1;
                    x.BlinkOffset = 0;
                    x.BlinkLength = 0.5f;
                    x.Color = Color.Red;
                });
            }

            private void ReadyLights(List<IMyLightingBlock> lights) {
                lights.ForEach(x => {
                    x.Enabled = true;
                    x.BlinkIntervalSeconds = 0;
                    x.BlinkOffset = 0;
                    x.BlinkLength = 0;
                    x.Color = Color.Green;
                });
            }
        }

        //to this comment.
        #region post-script
    }
}
#endregion