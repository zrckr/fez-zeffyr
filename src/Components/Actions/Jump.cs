using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class Jump : PlayerAction
    {
        [Export] public float MinimumHeight = 1f;
        [Export] public float MaximumHeight = 2.5f;
        [Export] public float JumpTime = 0.6f;
        [Export] public float SideFactor = 0.25f;

        public static JumpInfo Default;
        public static JumpInfo Carry;
        
        private const float SwimFriction = 0.775f;

        private ActionType[] _actions = new ActionType[]
        {
            ActionType.Slide, ActionType.CornerGrab, ActionType.Run,
            ActionType.Walk, ActionType.Land,
            ActionType.CarryIdle, ActionType.CarryWalk, ActionType.CarrySlide,
            ActionType.CarryHeavyIdle, ActionType.CarryHeavyWalk, ActionType.CarryHeavySlide
        };

        public override void _Ready()
        {
            base._Ready();
            Default = new JumpInfo(MinimumHeight, MaximumHeight, JumpTime);
            Carry = new JumpInfo(MinimumHeight / 2f, MaximumHeight / 2f, JumpTime);
        }

        protected override void TransitionAttempts()
        {
            if (!Player.Action.In(_actions) &&
                !Player.Action.IsClimbing() &&
                !Player.Action.IsSwimming() && !Player.Action.IsIdle() &&
                (Player.Action != ActionType.Fall || !Player.CanDoubleJump) &&
                (Player.Action != ActionType.Grab && Player.Action != ActionType.Push) &&
                !Player.Action.IsLooking() || 
                !FzInput.IsJustPressed(InputAction.Jump) &&
                (!Player.OnFloor && !Player.Action.IsOnLedge() || Player.Velocity.y <= 0.1f))
                return;

            Player.PushedBody = null;
            if (Player.CanDoubleJump) Player.CanDoubleJump = false;

            if (FzInput.AnyPressed(InputAction.Down) && (Player.OnFloor &&
                Player.Floor.GetLayer().HasFlag(PhysicsLayer.TopOnly) || Player.IsClimbing))
                return;

            if (Player.Action == ActionType.CornerGrab)
            {
                Direction dir = FzInput.Movement.x.GetDirectionFrom();
                if (dir != Direction.None && dir != Player.FacingDirection)
                {
                    Player.Origin -= Player.Axes.Facing;
                    Player.InBackground = PhysicsHelper.DetermineInBackground(Player);
                }
            }

            bool? anyCarried = (Player.CarriedBody is null) ? new bool?() : Player.CarriedBody.IsHeavy;
            Player.Action = anyCarried switch
            {
                false => ActionType.CarryJump,
                true => ActionType.CarryHeavyJump,
                null => ActionType.Jump,
            };
        }

        protected override void OnEnter()
        {
            Vector3 velocity = Player.Velocity;
            if (Player.CarriedBody != null)
            {
                AnimationPlayer.Advance(0.35f);
                velocity.y = Carry.Jump;
            }
            else
            {
                bool held = Player.Action.IsClimbing() || Player.HeldBody != null;
                velocity.x = (held) ? velocity.x + FzInput.Movement.x * SideFactor : velocity.x;
                velocity.y = Default.Jump * ((held || Player.IsSwimming) ? SwimFriction : 1f);
            }
           
            Player.Velocity = velocity;
            Signals.EmitSignal(nameof(ActionSignals.Jumped));
        }

        protected override void OnAct(float delta)
        {
            if (Player.OnFloor)
            {
                if (Player.CarriedBody != null)
                    WalkRun.Carry.Update(delta, FzInput.Movement.x);
                else
                    WalkRun.Default.Update(delta, FzInput.Movement.x);
            }
            
            if (FzInput.IsReleased(InputAction.Jump))
            {
                float termination = (Player.CarriedBody != null) ? Carry.Termination : Default.Termination;
                Player.Velocity = new Vector3()
                {
                    x = Player.Velocity.x,
                    y = Mathf.Min(termination, Player.Velocity.y)
                };
            }

            if (Player.CarriedBody != null)
            {
                AnimationPlayer.PlaybackSpeed = 0.5f;
                if (Player.OnFloor)
                {
                    FzInput.Joypad.Vibrate(Joypad.Motors.LeftStrong, 0.4f, 0.15f);
                    Player.Action = !Player.CarriedBody.IsHeavy ? ActionType.CarryIdle : ActionType.CarryHeavyIdle;
                }
            }
            else if (Player.Velocity.y < 0f)
            {
                Player.Action = ActionType.Fall;
            }
        }

        protected override bool IsAllowed(ActionType type) =>
            type.In(ActionType.Jump, ActionType.CarryJump, ActionType.CarryHeavyJump);
    }
}