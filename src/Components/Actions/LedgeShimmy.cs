using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class LedgeShimmy : PlayerAction
    {
        [Export(PropertyHint.Range, "0.1,1.0,0.05")]
        public float ShimmyingFactor = 0.15f;
        
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
                    Player.InBackground = !Player.InBackground;
                    Player.Action = Player.Action.FacesBack() ? ActionType.LedgeGrabFront : ActionType.LedgeGrabBack;
                }
                else
                {
                    if (Player.Action.FacesBack())
                        Player.FacingDirection = Mathf.Sign(distance) > 0 ? Direction.Left : Direction.Right;
                    else
                        Player.FacingDirection = Mathf.Sign(distance) > 0 ? Direction.Right : Direction.Left;
                    
                    Player.Action = ActionType.CornerGrab;
                    Player.InBackground = false;
                    AnimationPlayer.Seek(AnimationPlayer.CurrentAnimationLength);
                }
                _rotatedFrom = null;
            }
        }

        protected override void OnAct(float delta)
        {
            Player.Velocity = Vector3.Right * (FzInput.Movement.x * Player.DefaultSpeed * ShimmyingFactor);
            
            Transform transform = Player.GlobalTransform;
            transform.origin += Player.GlobalVelocity.Sign() * Player.Size.x;
            
            var pair =
                SpaceState.CastRect(transform, Player.Size * 1.5f, PhysicsLayer.TopOnly);
            
            if (pair.First != null && !Mathf.IsZeroApprox(FzInput.Movement.x))
            {
                Vector3 extents = pair.First.ShapeOwnerGetXFormExtents(0, Player.Transform.basis);
                ulong id1 = pair.First.GetInstanceId();
                ulong id2 = Player.HeldBody.GetInstanceId();
                
                if (pair.Far is null && id2 == id1)
                {
                    float diff = Player.Origin.Dot(Player.Axes.Right) -
                                 Player.HeldBody.GlobalTransform.origin.Dot(Player.Axes.Right);
                    if (Mathf.Abs(diff) > extents.x + Mathz.Trixel)
                    {
                        Player.Action = Player.Action.FacesBack() ? ActionType.ToCornerBack : ActionType.ToCornerFront;
                        Player.FacingDirection = Player.FacingDirection.GetOpposite();
                        return;
                    }
                }
                else
                {
                    Player.Origin = Player.Axes.XMask * Player.Origin +
                                    Player.Axes.Up * extents.y +
                                    Player.Axes.YzMask * pair.First.GlobalTransform.origin +
                                    Player.Axes.Forward * (extents.z + Player.Size.z + Mathz.HalfTrixel);
                    Player.HeldBody = pair.First;
                }
            } 
            else if (pair.First is null)
            {
                Player.Action = ActionType.DropDown;
                Player.Velocity = new Vector3(Player.Velocity.x, PhysicsHelper.Snap, 0f);
                Player.HeldBody = null;
                return;
            }
            
            Animation anim = AnimationPlayer.GetAnimation(Player.Action.GetAnimationName());
            if (!Mathf.IsZeroApprox(Player.Velocity.x))
            {
                anim.Loop = true;
                if (!AnimationPlayer.IsPlaying()) AnimationPlayer.Play();
                AnimationPlayer.PlaybackSpeed = Mathf.Abs(Player.Velocity.x);
            }
            else
            {
                anim.Loop = false;
                AnimationPlayer.Seek(0, true);
            }
        }

        protected override bool IsAllowed(ActionType type)
            => type == ActionType.ShimmyFront || type == ActionType.ShimmyBack;
    }
}