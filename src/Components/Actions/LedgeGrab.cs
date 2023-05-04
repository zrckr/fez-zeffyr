using System;
using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class LedgeGrab : PlayerAction
    {
        [Export] public float VelocityThreshold = 0.025f;
        [Export] public float MovementThreshold = 0.1f;
        [Export] public float DistanceThreshold = 0.35f;
        [Export] public float LeaveFloorThreshold = 1.25f;
        
        private Orthogonal? _rotatedFrom;

        public override void _Ready()
        {
            base._Ready();
            GameCamera.Connect(nameof(BaseCamera.PreRotate), this, nameof(_OnPreRotate));
            GameCamera.Connect(nameof(BaseCamera.Rotating), this, nameof(_OnRotating));
        }
        
        private void _OnPreRotate()
        {
            _rotatedFrom = null;
            if (IsAllowed(Player.Action) && (GameCamera.Orthogonal != GameCamera.LastOrthogonal) && !Player.Respawn.Used)
            {
                _rotatedFrom = GameCamera.LastOrthogonal;
                Player.Velocity = Vector3.Zero;
            }
        }

        private void _OnRotating(float step)
        {
            if (_rotatedFrom.HasValue && step >= 0.6f)
            {
                int distance = GameCamera.Orthogonal.DistanceTo(_rotatedFrom.Value);
                if (Mathf.Abs(distance) % 2 == 0)
                {
                    Player.FacingDirection = Player.FacingDirection.GetOpposite();
                }
                else
                {
                    if (Player.FacingDirection == Direction.Right)
                        Player.Action = Mathf.Sign(distance) > 0 ? ActionType.LedgeGrabBack : ActionType.LedgeGrabFront;
                    else
                        Player.Action = Mathf.Sign(distance) > 0 ? ActionType.LedgeGrabFront : ActionType.LedgeGrabBack;
                    
                    Player.InBackground = (Player.Action != ActionType.LedgeGrabBack);
                    AnimationPlayer.Seek(AnimationPlayer.CurrentAnimationLength);
                }
                _rotatedFrom = null;
            }
            
            if (Player.Action.IsOnLedge())
            {
                if (Player.HeldBody == null)
                    Player.Action = ActionType.Idle;
                else if (Player.HeldBody is RigidBody rigid && Mathf.Abs(rigid.LinearVelocity.Dot(Vector3.One)) > 0.5f)
                {
                    Player.GlobalVelocity = rigid.LinearVelocity;
                    Player.HeldBody = null;
                    Player.Action = ActionType.Jump;
                }
            }
        }
        
        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.Jump:
                case ActionType.Fall:
                    PhysicsBody rect = SpaceState.CastRect<PhysicsBody>(Player.GlobalTransform, Player.Size, 
                        PhysicsLayer.TopOnly).First;

                    Transform transform = Player.GlobalTransform;
                    transform.origin += Player.Axes.Facing * Player.Size.x;
                    PhysicsBody point =
                        SpaceState.CastPoint<PhysicsBody>(transform, PhysicsLayer.TopOnly);

                    if (CheckLedge(rect))
                    {
                        Player.HeldBody = rect;
                        Player.Action = ActionType.LedgeGrabBack;
                    }
                    else if (CheckCorner(rect, point))
                    {
                        Player.HeldBody = point;
                        Player.Action = ActionType.CornerGrab;
                        
                        FzInput.Joypad.Vibrate(Joypad.Motors.LeftStrong, 0.1f, 0.2f);
                        FzInput.Joypad.Vibrate(Joypad.Motors.RightWeak, 0.4f, 0.2f);
                    }
                    break;
            }
        }
        
        private bool CheckLedge(CollisionObject ledge)
        {
            if (!FzInput.IsPressed(InputAction.Up))
                return false;
            
            if (ledge != null)
            {
                Vector3 diff = ((ledge.GlobalTransform.origin - Player.Respawn.FloorOriginOnLeave) * Player.Axes.XyMask);
                return (ledge.Visible && diff.Length() >= 1.25f && Player.Action != ActionType.Jump);
            }
            return false;
        }

        private bool CheckCorner(CollisionObject empty, CollisionObject corner)
        {
            Direction facing = Player.FacingDirection;
            float vel = Player.Velocity.x * facing.Sign();
            float move = FzInput.Movement.x * facing.Sign();
            
            if (vel <= VelocityThreshold && move <= MovementThreshold) return false;
            if (empty != null || corner == null) return false;
            
            float y1 = corner.GlobalTransform.origin.Dot(Player.Axes.Up);
            float y2 = Player.Origin.Dot(Player.Axes.Up);
            
            if (!corner.Visible || y1 >= y2 || corner.GetLayer() != PhysicsLayer.TopOnly)
                return false;
            
            Vector3 vector;
            if (Player.Action == ActionType.Jump)
            {
                vector = (corner.GlobalTransform.origin - Player.Respawn.FloorOriginOnLeave) * Player.Axes.XyMask;
                if (vector.Length() < LeaveFloorThreshold)
                    return false;
            }
            
            Vector3 extents = corner.ShapeOwnerGetXFormExtents(0, Player.Transform.basis);
            Vector3 offset = (Player.Axes.Right * -facing.Sign() + Player.Axes.Up * extents);
            
            vector = Player.Origin * Player.Axes.XyMask - (corner.GlobalTransform.origin * Player.Axes.XyMask + offset);
            if (vector.Length() >= DistanceThreshold) return false;
            
            FzInput.Joypad.Vibrate(Joypad.Motors.LeftStrong, 0.1f, 0.2f);
            FzInput.Joypad.Vibrate(Joypad.Motors.RightWeak, 0.4f, 0.2f);
            
            return true;
        }

        protected override void OnEnter()
        {
            if (Player.LastAction == ActionType.Fall || Player.LastAction == ActionType.Jump)
            {
                Vector3 extents = Player.HeldBody.ShapeOwnerGetXFormExtents(0, Player.Transform.basis);

                Vector3 y = Player.Axes.YMask * Player.HeldBody.GlobalTransform.origin + Player.Axes.Up * extents.y;
                Vector3 z = Player.Axes.ZMask * Player.HeldBody.GlobalTransform.origin +
                            Player.Axes.Forward * (extents.z + Player.Size.z + Mathz.HalfTrixel);

                Vector3 x = Player.Axes.XMask * Player.Origin;
                if (Player.Action == ActionType.CornerGrab)
                {
                    x = Player.Axes.XMask * Player.HeldBody.GlobalTransform.origin
                        - Player.Axes.Facing * (Player.Size.x + extents.x);
                }

                Player.Origin = x + y + z;
                Player.Velocity = Vector3.Zero;
                Signals.EmitSignal(nameof(ActionSignals.GrabbedLedge));
            }
        }

        protected override void OnAct(float delta)
        {
            if (Player.Action != ActionType.CornerGrab && !AnimationPlayer.IsPlaying())
                Player.Action = Player.Action.FacesBack() ? ActionType.ShimmyBack : ActionType.ShimmyFront;
            
            if (Player.Action == ActionType.CornerGrab)
            {
                Predicate<string> anyPressed = (action) =>
                    AnimationPlayer.IsPlaying() ? FzInput.IsJustPressed(action) : FzInput.IsPressed(action);
            
                bool right = Player.FacingDirection == Direction.Right && anyPressed(InputAction.Right);
                bool left = Player.FacingDirection == Direction.Left && anyPressed(InputAction.Left);

                if (!Player.InBackground && (right || left) && Player.HeldBody != null)
                {
                    Player.Action = ActionType.FromCornerBack;
                }
            }
        }

        protected override bool IsAllowed(ActionType type) =>
            type.In(ActionType.LedgeGrabFront, ActionType.LedgeGrabBack, ActionType.CornerGrab);
    }
}