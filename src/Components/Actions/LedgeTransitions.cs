using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class LedgeTransitions : PlayerAction
    {
        protected override void OnEnter()
        {
            Transform transform = Player.GlobalTransform;
            transform.origin += Player.Axes.Facing * Player.Size.x;
            CollisionObject obj = SpaceState.CastRect(transform, Player.Size, PhysicsLayer.TopOnly).First;
            if (obj != null)
                Player.HeldBody = obj;
            
            Vector3 extents = Player.HeldBody.ShapeOwnerGetXFormExtents(0, Player.Transform.basis);
            Vector3 opposite = -Player.Axes.Facing;
            
            Player.Velocity = Vector3.Zero;
            if (Player.Action == ActionType.FromCornerBack)
            {
                Player.Origin = Player.HeldBody.GlobalTransform.origin +
                                opposite * extents.x +
                                Player.Axes.Up * extents.y +
                                Player.Axes.Forward * (extents.z + Player.Size.z + Mathz.HalfTrixel);
            }
            else
            {
                Player.Origin = Player.HeldBody.GlobalTransform.origin +
                                opposite * (extents.x + Player.Size.x) +
                                Player.Axes.Up * extents.y * 
                                (Player.InBackground ? -1f : 1f);
            }
        }

        protected override void OnAct(float delta)
        {
            if (!AnimationPlayer.IsPlaying())
            {
                if (Player.Action == ActionType.FromCornerBack)
                {
                    Player.InBackground = false;
                    Player.Action = ActionType.ShimmyBack;
                }
                else
                {
                    Player.Action = ActionType.CornerGrab;
                    AnimationPlayer.Seek(AnimationPlayer.CurrentAnimationLength, true);
                }
            }
        }

        protected override bool IsAllowed(ActionType type)
            => type.In(ActionType.FromCornerBack, ActionType.ToCornerFront, ActionType.ToCornerBack);
    }
}