using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class LedgeLowerTo : PlayerAction
    {
        private Vector3 _extents;

        protected override void TransitionAttempts()
        {
            if (Player.Floor is null) return;
            switch (Player.Action)
            {
                case ActionType.IdlePlay:
                case ActionType.IdleSleep:
                case ActionType.IdleLookAround:
                case ActionType.IdleYawn:
                case ActionType.Idle:
                case ActionType.LookLeft:
                case ActionType.LookRight:
                case ActionType.LookUp:
                case ActionType.LookDown:
                case ActionType.Walk:
                case ActionType.Run:
                case ActionType.Slide:
                case ActionType.Land:
                case ActionType.Teeter:
                    _extents = Player.Floor.ShapeOwnerGetXFormExtents(0, Player.Transform.basis);
                    Transform transform = Player.GlobalTransform;
                    PhysicsLayer noBackground = PhysicsLayer.Solid & ~PhysicsLayer.Background;

                    transform.origin = Player.Floor.GlobalTransform.origin -
                                       Player.Axes.Up * (_extents.y + 2f * Mathz.Trixel);
                    CollisionObject down = SpaceState.CastPoint(transform, noBackground);

                    transform.origin = Player.Origin
                                       + Player.Axes.Facing * (Player.Size.x + Mathz.HalfTrixel)
                                       - Player.Axes.Up * (Player.Size.y + Mathz.HalfTrixel);
                    CollisionObject downSide = SpaceState.CastPoint(transform, noBackground);

                    if (CheckStraight(down, downSide))
                    {
                        Player.HeldBody = Player.Floor;
                        Player.Action = ActionType.LedgeLowerTo;
                    }
                    else if (CheckCorner(down, downSide))
                    {
                        WalkTo.NextOrigin = GetEdgePosition;
                        WalkTo.NextAction = ActionType.CornerLowerTo;
                        Player.Action = ActionType.WalkTo;
                        Player.HeldBody = Player.Floor;
                    }

                    break;
            }
        }

        private bool CheckStraight(CollisionObject down, CollisionObject downSide)
        {
            if (Player.InBackground
                || (!FzInput.IsJustPressed(InputAction.Jump) || !FzInput.AnyPressed(InputAction.Down))
                && (!Player.Action.IsOnLedge() || !FzInput.IsJustPressed(InputAction.Down))
                || !Player.Floor.GetLayer().HasFlag(PhysicsLayer.TopOnly)
                || !Mathf.IsZeroApprox(FzInput.Movement.x))
                return false;

            if (down != null && downSide != null)
            {
                Vector3 x = Player.Axes.Right * Player.Origin;
                Vector3 y = Player.Axes.Up * Player.Origin - Player.Axes.Up * Mathz.Trixel;
                Vector3 z = Player.Axes.Forward * Player.Floor.GlobalTransform.origin + Player.Axes.Forward;

                Player.Origin = x + y + z;
                Player.Velocity = Vector3.Up * PhysicsHelper.Snap;
                return false;
            }

            return true;
        }

        private bool CheckCorner(CollisionObject down, CollisionObject far)
        {
            if (!FzInput.IsJustPressed(InputAction.Down))
                return false;
            if (down != null || far != null || !Player.Floor.GetLayer().HasFlag(PhysicsLayer.TopOnly))
                return false;
            return true;
        }

        private Vector3 GetEdgePosition()
        {
            return Player.HeldBody.GlobalTransform.origin +
                   Player.Axes.Facing * (_extents.x - Player.Size.x) +
                   Player.Axes.Up * (_extents.y + Player.Size.y);
        }

        protected override void OnEnter()
        {
            Player.Origin += Player.Axes.Forward * _extents.z;
            Player.Velocity = Vector3.Zero;
        }

        private void OnAnimationEnd()
        {
            Vector3 y = Player.Axes.YMask * Player.HeldBody.GlobalTransform.origin +
                        Player.Axes.Up * _extents.y;

            Vector3 z = Player.Axes.ZMask * Player.HeldBody.GlobalTransform.origin +
                        Player.Axes.Forward * (_extents.z + Player.Size.z + Mathz.HalfTrixel);

            Vector3 x = Player.Axes.XMask * Player.Origin;
            if (Player.Action == ActionType.CornerLowerTo)
                x = Player.Axes.XMask * Player.Origin +
                    Player.Axes.Facing * Player.Size.x * 2f;

            Player.Origin = x + y + z;
        }

        protected override void OnAct(float delta)
        {
            Player.Velocity = Vector3.Zero;
            if (Player.HeldBody == null)
            {
                Player.Action = ActionType.Idle;
                return;
            }
            
            if (!AnimationPlayer.IsPlaying())
            {
                OnAnimationEnd();
                if (Player.Action == ActionType.CornerLowerTo)
                {
                    Player.FacingDirection = Player.FacingDirection.GetOpposite();
                    Player.Action = ActionType.CornerGrab;
                    AnimationPlayer.Advance(AnimationPlayer.CurrentAnimationLength);
                }
                else
                {
                    Player.Action = ActionType.ShimmyBack;
                }
            }
        }

        protected override bool IsAllowed(ActionType type) =>
            type == ActionType.LedgeLowerTo || type == ActionType.CornerLowerTo;
    }
}