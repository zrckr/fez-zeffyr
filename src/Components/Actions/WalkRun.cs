using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class WalkRun : PlayerAction
    {
        [Export(PropertyHint.Range, "0.1,1.0,0.1")]
        public float RunFactor = 1f;

        [Export(PropertyHint.Range, "0.1,1.0,0.1")]
        public float WalkFactor = 0.8f;

        [Export(PropertyHint.Range, "1,100,1")]
        public float Acceleration = 20f;

        public static MoveHelper Default;
        public static MoveHelper Carry;

        private const float RunAnimationSpeed = 1.25f;

        public override void _Ready()
        {
            base._Ready();
            float walk = WalkFactor * Player.DefaultSpeed;
            float run = RunFactor * Player.DefaultSpeed;

            Default = new MoveHelper(Player, walk, run, Acceleration);
            Carry = new MoveHelper(Player, 0f, 0f, float.MaxValue);
        }

        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.LookUp:
                case ActionType.LookDown:
                case ActionType.LookLeft:
                case ActionType.LookRight:

                case ActionType.Slide:
                case ActionType.CarrySlide:
                case ActionType.CarryHeavySlide:

                case ActionType.Grab:
                case ActionType.Push:
                case ActionType.Teeter:

                case ActionType.Idle:
                case ActionType.IdlePlay:
                case ActionType.IdleSleep:
                case ActionType.IdleLookAround:
                case ActionType.IdleYawn:
                case ActionType.CarryIdle:
                case ActionType.CarryHeavyIdle:
                    if (Player.OnFloor && FzInput.Movement.x != 0.0 && Player.PushedBody == null)
                    {
                        bool? anyCarried = (Player.CarriedBody is null) ? new bool?() : Player.CarriedBody.IsHeavy;
                        Player.Action = anyCarried switch
                        {
                            false => ActionType.CarryWalk,
                            true => ActionType.CarryHeavyWalk,
                            null => ActionType.Walk,
                        };
                    }

                    Default.Reset();
                    Carry.Reset();
                    break;
            }
        }

        protected override void OnEnter()
        {
            Default.Reset();
            Carry.Reset();
        }

        protected override void OnAct(float delta)
        {
            switch (Player.Action)
            {
                case ActionType.Walk:
                case ActionType.Run:
                    ActWalkRun(delta);
                    break;

                case ActionType.CarryWalk:
                case ActionType.CarryHeavyWalk:
                    ActCarryWalk(delta);
                    break;
            }
        }

        private void ActWalkRun(float delta)
        {
            if (Default.IsRunning)
            {
                Player.Action = ActionType.Run;
                AnimationPlayer.PlaybackSpeed = RunAnimationSpeed;
            }
            else
            {
                Player.Action = ActionType.Walk;
                AnimationPlayer.PlaybackSpeed = Easing.Out.Cubic(Mathf.Min(1f, Mathf.Abs(FzInput.Movement.x)));
            }

            Default.Update(delta, FzInput.Movement.x);
        }

        private void ActCarryWalk(float delta)
        {
            float factor = !Player.CarriedBody.IsHeavy ? Player.LightFactor : Player.HeavyFactor;
            Carry.WalkSpeed = Player.DefaultSpeed * WalkFactor * factor;
            Carry.Update(delta, FzInput.Movement.x);
        }

        protected override bool IsAllowed(ActionType type) =>
            type.In(ActionType.Run, ActionType.Walk, ActionType.RunSwitch, ActionType.CarryWalk,
                ActionType.CarryHeavyWalk);
    }
}