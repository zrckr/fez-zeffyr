using System;
using System.Collections.Generic;
using Zeffyr.Tools;

namespace Zeffyr.Structure
{
    public enum ActionType
    {
        None,

        Idle,
        IdleLookAround,
        IdlePlay,
        IdleSleep,
        IdleYawn,
        CarryIdle,
        CarryHeavyIdle,
        Teeter,
        TeeterPaul,

        WalkTo,
        Walk,
        Run,
        RunSwitch,
        CarryWalk,
        CarryHeavyWalk,
        Slide,
        CarrySlide,
        CarryHeavySlide,

        Jump,
        CarryJump,
        CarryHeavyJump,
        Bounce,
        Fly,
        Fall,
        Land,
        DropDown,

        AirPanic,
        Dying,
        SuckedIn,
        Hurt,
        CrushHorz,
        CrushVert,

        Grab,
        Push,
        GrabTombstone,
        LetGoOfTombstone,
        PivotTombstone,
        PushPivot,
        HitBell,
        TurnAwayFromBell,
        TurnToBell,
        OpenDoor,
        LiftTrile,
        ThrowTrile,
        DropTrile,
        LiftHeavy,
        ThrowHeavy,
        DropHeavy,

        LookUp,
        LookDown,
        LookLeft,
        LookRight,

        DrumsCrash,
        DrumsHiHat,
        DrumsIdle,
        DrumsTom,
        DrumsTom2,
        DrumsToss,
        DrumsTwirl,

        EnterDoor,
        EnterDoorSpin,
        EnterPipe,
        EnterTunnel,
        ExitDoor,
        CarryEnter,
        CarryEnterDoorSpin,
        CarryEnterTunnel,
        CarryExit,
        CarryHeavyEnter,
        CarryHeavyEnterDoorSpin,
        CarryHeavyEnterTunnel,
        CarryHeavyExit,
        GateWarp,

        IdleToClimbBack,
        IdleToClimbFront,
        IdleToClimbSide,
        ClimbFront,
        ClimbBack,
        ClimbSide,
        ClimbFrontSideways,
        ClimbBackSideways,
        ClimbOver,
        JumpToClimb,
        JumpToClimbSide,

        CornerPullUp,
        CornerLowerTo,
        CornerGrab,
        LedgeGrabFront,
        LedgeGrabBack,
        LedgePullUpFront,
        LedgePullUpBack,
        LedgeLowerTo,
        ShimmyFront,
        ShimmyBack,
        ToCornerFront,
        ToCornerBack,
        FromCornerBack,

        Float,
        Swim,
        HurtSwim,
        Sink,
        
        FirstPerson,
        FindTreasure,
        OpenTreasure,
        ReadListen,
    }

    public static class ActionExtensions
    {
        private enum GroupType
        {
            None,
            Carry,
            Climb,
            Enter,
            Idle,
            Look,
            Ledge,
            Drums,
            Swim,
            Defeated
        }

        private sealed class Information
        {
            public string Animation { get; set; } = "_entry";

            public GroupType Group { get; set; } = GroupType.None;

            public bool AllowsDirectionChange { get; set; } = false;

            public bool IgnoresGravity { get; set; } = false;

            public bool DisallowsRespawn { get; set; } = false;

            public bool FacesBack { get; set; } = false;

            public bool HandlesDepthChange { get; set; } = false;

            public bool NeedsAlwaysOnTop { get; set; } = true;

            public bool NoBackgroundDarkening { get; set; } = false;

            public bool PreventsFall { get; set; } = false;

            public bool? PreventsRotation { get; set; } = null;

            public bool SkipSilhouette { get; set; } = false;
        }

        private static readonly Dictionary<ActionType, Information> Infos;

        private const string ActionsPath = "res://assets/ActionProperties.json";

        static ActionExtensions()
        {
            if (FileOperations.TryLoadJson(ActionsPath, out Dictionary<string, Information> dictionary))
            {
                if (dictionary == null)
                    throw new NullReferenceException($"Invalid JSON file: {ActionsPath}");
                
                Infos = new Dictionary<ActionType, Information>();
                foreach (string key in dictionary.Keys)
                {
                    Enum.TryParse(key, out ActionType type);
                    Infos[type] = dictionary[key];
                }
            }
        }

        public static string GetAnimationName(this ActionType type)
            => Infos[type].Animation;

        public static bool AllowsDirectionChange(this ActionType type)
            => Infos[type].AllowsDirectionChange;

        public static bool IgnoresGravity(this ActionType type)
            => Infos[type].IgnoresGravity;

        public static bool DisallowsRespawn(this ActionType type)
            => Infos[type].DisallowsRespawn;

        public static bool FacesBack(this ActionType type)
            => Infos[type].FacesBack;

        public static bool HandlesDepthChange(this ActionType type)
            => Infos[type].HandlesDepthChange;

        public static bool NeedsAlwaysOnTop(this ActionType type)
            => Infos[type].NeedsAlwaysOnTop;

        public static bool NoBackgroundDarkening(this ActionType type)
            => Infos[type].NoBackgroundDarkening;

        public static bool PreventsFall(this ActionType type)
            => Infos[type].PreventsFall;

        public static bool PreventsRotation(this ActionType type)
        {
            bool? val = Infos[type].PreventsRotation;
            if (val.HasValue)
                return val.Value;
            else
                return !Infos[type].AllowsDirectionChange;
        }

        public static bool SkipSilhouette(this ActionType type)
            => Infos[type].SkipSilhouette;

        public static bool WithoutGroup(this ActionType type)
            => Infos[type].Group == GroupType.None;

        public static bool IsCarrying(this ActionType type)
            => Infos[type].Group == GroupType.Carry;

        public static bool IsClimbing(this ActionType type)
            => Infos[type].Group == GroupType.Climb;

        public static bool IsEnteringDoor(this ActionType type)
            => Infos[type].Group == GroupType.Enter;

        public static bool IsIdle(this ActionType type)
            => Infos[type].Group == GroupType.Idle;

        public static bool IsLooking(this ActionType type)
            => Infos[type].Group == GroupType.Look;

        public static bool IsOnLedge(this ActionType type)
            => Infos[type].Group == GroupType.Ledge;

        public static bool IsPlayingDrums(this ActionType type)
            => Infos[type].Group == GroupType.Drums;

        public static bool IsSwimming(this ActionType type)
            => Infos[type].Group == GroupType.Swim;

        public static bool IsAlive(this ActionType type)
            => Infos[type].Group != GroupType.Defeated;
    }
}