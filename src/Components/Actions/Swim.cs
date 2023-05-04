using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class Swim : PlayerAction
    {
        [Export] public float PulseDelay = 0.5f;
        [Export] public float Flotation = 0.006f;
        [Export] public float MaxSubmergedPortion = 0.5f;
        [Export] public float SwimFactor = 1f;

        [Export] public AudioStreamSample SwimSound;
        [Export] public AudioStreamSample FloatSound;

        public const float UpFactor = 0.4725f;
        public const float DiffFactor = 0.025f;
        public const float StableFactor = 3f / 500f;
        public const float MaxVelocity = 0.2f;

        private float _pulseElapsed;
        private MoveHelper _swimming;

        public override void _Ready()
        {
            base._Ready();
            _swimming = new MoveHelper(Player, SwimFactor * Player.DefaultSpeed, 
                SwimFactor * Player.DefaultSpeed, 1f);
        }

        private float SubmergedPortion
            => Level.Water.Type.In(LiquidType.Lava, LiquidType.Sewer) ? 0.25f : 0.5f;

        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.Swim:
                case ActionType.Dying:
                case ActionType.Sink:
                case ActionType.Float:
                case ActionType.HurtSwim:
                case ActionType.SuckedIn:
                    break;
                default:
                    if (Player.Action != ActionType.Jump &&
                        Level.Water != null &&
                        Player.Origin.y < Level.Water.Height - SubmergedPortion)
                    {
                        Player.Respawn.Record();
                        Player.Action = ActionType.Float;
                        Player.Velocity *= new Vector3(1f, 0.25f, 1f);
                    }
                    break;
            }
        }

        protected override void OnEnter()
        {
            if (Player.CarriedBody != null)
            {
                Player.CarriedBody.EnableCollision = true;
                Player.CarriedBody = null;
            }
        }

        protected override void OnAct(float delta)
        {
            float height = Level.Water.Height - SubmergedPortion;

            if (!Mathf.IsEqualApprox(Player.Origin.y, height))
            {
                if (Level.Water.Type == LiquidType.Lava || Level.Water.Type == LiquidType.Sewer)
                {
                    Player.Action = ActionType.Sink;
                    return;
                }
                
                float diff = height - Player.Origin.y;
                Player.Velocity += UpFactor * delta * Vector3.Up;

                if (diff > DiffFactor)
                    Player.Velocity += diff * StableFactor * Vector3.Up;
                else
                    Player.Origin = Player.Origin * Player.Axes.XzMask + Player.Axes.Up * height;
            }
            else if (Mathf.Abs(Player.Velocity.y) > MaxVelocity || Player.OnFloor)
            {
                Player.Action = ActionType.Fall;
                return;
            }

            _pulseElapsed -= delta;
            if (Mathf.IsZeroApprox(FzInput.Movement.x) && Player.Action != ActionType.HurtSwim)
            {
                Player.Action = ActionType.Float;
            }
            else
            {
                if (Player.Action != ActionType.HurtSwim)
                    Player.Action = ActionType.Swim;
                if (_pulseElapsed <= 0f)
                {
                    _pulseElapsed = 0.5f;
                    AudioPlayer.Stream = SwimSound;
                    AudioPlayer.Play();
                }

                float ease = Easing.In.Sine(_pulseElapsed);
                _swimming.Update(ease, FzInput.Movement.x);

                CollisionObject obj = Player.Wall.Collider;
                if (obj is Pickup pickup)
                {
                    Player.Velocity *= new Vector3(0.9f, 1f, 0.9f);
                    pickup.Velocity += Player.Velocity * Player.Axes.XzMask;
                }
            }
        }

        protected override void OnEnd()
        {
            _pulseElapsed = 0f;
            if (!Player.Action.In(ActionType.Hurt, ActionType.Sink, ActionType.Jump, ActionType.Fly) && 
                Level.Water != null)
            {
                Player.Velocity *= new Vector3(1f, 0.5f, 1f);
            }
        }

        protected override bool IsAllowed(ActionType type) => type.IsSwimming();
    }
}
