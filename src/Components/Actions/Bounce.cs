using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class Bounce : PlayerAction
    {
        [Export] public AudioStreamSample BounceHighSound;
        [Export] public AudioStreamSample BounceLowSound;

        private const float BounceVibrate = 0.3f;
        private const float BounceProbability = 0.5f;
        private const float BounceResponse = 0.32f;
        
        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.Idle:
                case ActionType.Walk:
                case ActionType.Run:
                case ActionType.Jump:
                case ActionType.Fall:
                case ActionType.DropDown:
                case ActionType.Slide:
                case ActionType.Land:
                case ActionType.Teeter:
                case ActionType.IdlePlay:
                case ActionType.IdleSleep:
                case ActionType.IdleLookAround:
                case ActionType.IdleYawn:
                    if (Player.OnFloor && Player.Floor.GetLayer().HasFlag(PhysicsLayer.Bounce))
                    {
                        Player.Action = ActionType.Bounce;
                    }
                    break;
            }
            if (Player.Action == ActionType.Bounce)
            {
                Player.Action = ActionType.Land;
            }
        }

        protected override void OnEnter()
        {
            FzInput.Joypad.Vibrate(Joypad.Motors.LeftStrong, 0.5f, BounceVibrate, Easing.In.Quad);
            FzInput.Joypad.Vibrate(Joypad.Motors.RightWeak, 0.6f, BounceVibrate, Easing.In.Quad);
            
            AudioPlayer.Stream = Random.Probability(BounceProbability) ? BounceHighSound : BounceLowSound;
            AudioPlayer.Play();

            Player.Velocity = new Vector3()
            {
                x = Player.Velocity.x,
                y = Jump.Default.Jump * (1f + BounceResponse)
            };
        }

        protected override bool IsAllowed(ActionType type) => type == ActionType.Bounce;
    }
}