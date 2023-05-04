using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class LedgePullUp : PlayerAction
    {
        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.ShimmyFront:
                case ActionType.ShimmyBack:
                    if ((!FzInput.IsJustPressed(InputAction.Jump) || FzInput.AnyPressed(InputAction.Down))
                        && !FzInput.IsJustPressed(InputAction.Up) || Player.HeldBody == null)
                        break;
                    
                    Player.Action = Player.Action.FacesBack() ? ActionType.LedgePullUpBack : ActionType.LedgePullUpFront;
                    break;
                
                case ActionType.CornerGrab:
                    if ((!FzInput.IsJustPressed(InputAction.Jump)
                         || Mathf.IsEqualApprox(FzInput.Movement.x, -Player.FacingDirection.Sign())
                         || FzInput.AnyPressed(InputAction.Down))
                        && !FzInput.IsJustPressed(InputAction.Up)
                        && (!FzInput.IsPressed(InputAction.Up) || AnimationPlayer.IsPlaying() || Player.LastAction == ActionType.ClimbSide)
                        || Player.HeldBody == null)
                        break;
                    
                    Player.Action = ActionType.CornerPullUp;
                    break;
            }
        }

        protected override void OnEnter()
        {
            Vector3 y = Player.Axes.YMask * Player.HeldBody.GlobalTransform.origin + 
                        Player.Axes.Up * Player.Size.y * 2f;
            
            Vector3 z = Player.Axes.ZMask * Player.HeldBody.GlobalTransform.origin +
                        Player.Axes.Forward * Player.Size.x;

            Vector3 x = Player.Axes.XMask * Player.Origin;
            if (Player.LastAction == ActionType.CornerGrab)
            {
                x = Player.Axes.XMask * Player.Origin +
                    Player.Axes.Facing * Player.Size.x * 2f;
            }
            
            Player.Origin = x + y + z;
            Signals.EmitSignal(nameof(ActionSignals.Hoisted));
        }

        protected override void OnAct(float delta)
        {
            if (!AnimationPlayer.IsPlaying())
            {
                Player.Velocity = Vector3.Up * PhysicsHelper.Snap;
                Player.HeldBody = null;
                Player.Action = ActionType.Idle;
            }
        }

        protected override bool IsAllowed(ActionType type)
            => type.In(ActionType.CornerPullUp, ActionType.LedgePullUpBack, ActionType.LedgePullUpBack);
    }
}