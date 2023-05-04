using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class Teeter : PlayerAction
    {
        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.Idle:
                case ActionType.Walk:
                case ActionType.Run:
                case ActionType.DropDown:
                case ActionType.Slide:
                case ActionType.Grab:
                case ActionType.Push:
                
                case ActionType.IdlePlay:
                case ActionType.IdleSleep:
                case ActionType.IdleLookAround:
                case ActionType.IdleYawn:
                    if (Player.PushedBody != null || Player.CarriedBody != null ||
                        Player.Floor == null || FzInput.Movement.x != 0.0)
                        break;

                    Vector3 r = Player.Axes.Right;
                    Vector3 p = Player.Origin;
                    Vector3 f = Player.Floor.GlobalTransform.origin;
                    float diff = Mathf.Abs(f.Dot(r) - p.Dot(r));
                    
                    Transform transform = Player.GlobalTransform;

                    transform.origin += Player.Axes.Facing * (Player.Size.x + Mathz.HalfTrixel);
                    transform.origin -= Player.Axes.Up * (Player.Size.y + Mathz.HalfTrixel);
                    bool empty = SpaceState.CastPoint(transform, PhysicsLayer.Solid) is null;
                    
                    if (diff > 0.45 && diff <= 1.0 && empty)
                    {
                        Player.Velocity = new Vector3(0.0f, Player.Velocity.y, Player.Velocity.z);
                        Player.Action = ActionType.Teeter;
                    }
                    break;
            }
        }

        protected override bool IsAllowed(ActionType type) =>
            type.In(ActionType.Teeter, ActionType.TeeterPaul);
    }
}
