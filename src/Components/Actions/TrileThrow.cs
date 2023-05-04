using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class TrileThrow : PlayerAction
    {
        [Export(PropertyHint.Range, "1, 10, 1")]
        public float ThrowStrength = 3f;

        private Spatial CarryOffset => GetNode<Spatial>("../../CarryOffset");

        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.CarryIdle:
                case ActionType.CarryWalk:
                case ActionType.CarryJump:
                case ActionType.CarrySlide:
                case ActionType.CarryHeavyIdle:
                case ActionType.CarryHeavyWalk:
                case ActionType.CarryHeavyJump:
                case ActionType.CarryHeavySlide:
                    if (Player.InBackground || !FzInput.IsJustPressed(InputAction.GrabThrow) ||
                        FzInput.IsPressed(InputAction.Down) &&
                        Mathf.IsEqualApprox(FzInput.Movement.LengthSquared(), 0f, 0.5f))
                        break;

                    Player.Action = Player.CarriedBody.IsHeavy ? ActionType.ThrowHeavy : ActionType.ThrowTrile;
                    break;
            }
        }

        protected override void OnEnter()
        {
            if (Player.CarriedBody is null)
                Player.Action = ActionType.Idle;
            else
                Signals.EmitSignal(nameof(ActionSignals.ThrewObject));
        }

        protected override void OnAct(float delta)
        {
            Vector3 offset = CarryOffset.Translation;
            if (Player.CarriedBody != null)
            {
                if (Player.CarriedBody.IsHeavy)
                    Player.Velocity *= Vector3.Up;
                
                CarryOffset.Translation = new Vector3()
                {
                    x = offset.x * Player.FacingDirection.Sign(),
                    y = offset.y
                };
                Player.CarriedBody.Origin = CarryOffset.GlobalTransform.origin;

                if (CarryOffset.Translation.IsZeroApprox())
                {
                    Player.CarriedBody.EnableCollision = true;
                    Player.CarriedBody.Velocity += new Vector3()
                    {
                        x = Player.FacingDirection.Sign() * ThrowStrength,
                        y = ThrowStrength
                    };
                
                    NodeExtensions.AdoptChild(Level, Player.CarriedBody);
                    Player.CarriedBody = null;
                }
            }
            
            if (!AnimationPlayer.IsPlaying())
            {
                Player.Action = ActionType.Idle;
            }
        }

        protected override bool IsAllowed(ActionType type) =>
            type == ActionType.ThrowTrile || type == ActionType.ThrowHeavy;
    }
}