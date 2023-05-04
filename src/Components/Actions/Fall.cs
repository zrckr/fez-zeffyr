using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;

namespace Zeffyr.Components.Actions
{
    public class Fall : PlayerAction
    {
        [Export] public float DoubleJumpTime = 0.1f;
        [Export] public float AirControl = 4f;

        protected override void OnAct(float delta)
        {
            float gravity = (Player.CarriedBody != null) ? Jump.Carry.Fall : Jump.Default.Fall;
            Vector3 velocity = Player.Velocity;

            if (!Player.OnFloor && Player.Action != ActionType.Hurt)
            {
                velocity.x = Mathf.Lerp(velocity.x, FzInput.Movement.x * Player.DefaultSpeed,  AirControl * delta);
                velocity.y = Mathf.Max(gravity, velocity.y + delta * gravity);
                
                if (Player.Action == ActionType.Hurt)
                    velocity.y /= 2f;

                Player.AirTime += delta;
                Player.CanDoubleJump &= Player.AirTime < DoubleJumpTime;
            }
            else
            {
                velocity.y = PhysicsHelper.Snap;
                Player.AirTime = 0f;
                Player.CanDoubleJump = true;
            }

            if (!Player.OnFloor && velocity.y <= 0.0f && Player.CarriedBody is null &&
                !Player.Action.PreventsFall() && Player.Action != ActionType.Fall &&
                !Player.LastAction.IsOnLedge())
            {
                Player.Action = ActionType.Fall;
            }

            if (Player.Action == ActionType.Fall && Player.OnFloor)
            {
                FzInput.Joypad.Vibrate(Joypad.Motors.RightWeak, 0.4f, 0.15f);
                Player.Action = ActionType.Land;
                AnimationPlayer.PlaybackSpeed = 1.75f;
                Signals.EmitSignal(nameof(ActionSignals.Landed));
            }

            if (Player.Action == ActionType.Land && !AnimationPlayer.IsPlaying())
            {
                Player.Action = ActionType.Idle;
            }
            Player.Velocity = velocity;
        }

        protected override bool IsAllowed(ActionType type)
            => !type.IgnoresGravity() && Player.Visible && !Debug.FreeMode;
    }
}