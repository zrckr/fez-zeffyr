using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class TrileDrop : PlayerAction
    {
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
                    bool drop = FzInput.Movement.y < -0.5f;
                    if (FzInput.IsPressed(InputAction.GrabThrow))
                        drop = Mathf.Abs(FzInput.Movement.x) < 0.25f;

                    if (!Player.InBackground && drop)
                    {
                        Player.Velocity *= Vector3.Up;
                        Player.Action = (Player.CarriedBody.IsHeavy) 
                            ? ActionType.DropHeavy : ActionType.DropTrile;
                    }
                    break;
            }
        }

        protected override void OnEnter()
        {
            Signals.EmitSignal(nameof(ActionSignals.DroppedObject));
        }

        protected override void OnAct(float delta)
        {
            if (Player.CarriedBody == null)
            {
                Player.Action = ActionType.Idle;
                return;
            }

            if (!AnimationPlayer.IsPlaying())
            {
                Player.CarriedBody.EnableCollision = true;
                Player.CarriedBody.Origin += Player.Axes.Facing * Mathz.HalfTrixel;
                NodeExtensions.AdoptChild(Level, Player.CarriedBody);

                if (Player.CarriedBody.IsHeavy)
                {
                    Player.PushedBody = Player.CarriedBody;
                    Player.Action = ActionType.Grab;
                    AnimationPlayer.Advance(AnimationPlayer.CurrentAnimationLength);
                }
                else
                {
                    Player.Action = ActionType.Idle;
                }
                Player.CarriedBody = null;
                Player.GlobalVelocity = PhysicsHelper.HugResponse(Player);
            }
        }

        protected override bool IsAllowed(ActionType type) 
            => type == ActionType.DropTrile || type == ActionType.DropHeavy;
    }
}