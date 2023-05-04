using Godot;
using System;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;
using Area = Godot.Area;
using Vector3 = Godot.Vector3;

namespace Zeffyr.Components.Actions
{
    public class Climb : PlayerAction
    {
        [Export] public float VineClimbFactor = 0.475f;
        [Export] public float LadderClimbFactor = 0.425f;
        [Export] public float OffsetY = 0.15f;
        [Export] public AudioStream ClimbVineSound;
        [Export] public AudioStream ClimbLadderSound;

        private bool _onSingleClimbable;
        private PhysicsLayer _currentLayer;
        private Direction _currentApproach;

        public override void _Ready()
        {
            base._Ready();
            GameCamera.Connect(nameof(BaseCamera.Rotating), this, nameof(OnRotating));
            GameCamera.Connect(nameof(BaseCamera.Rotated), this, nameof(OnRotated));
        }

        private void OnRotated()
        {
            if (IsAllowed(Player.Action) && GameCamera.LastOrthogonal != GameCamera.Orthogonal)
            {
                Direction[] list = {
                    Direction.Right, Direction.Backward, Direction.Left, Direction.Forward
                };
                
                int dist = GameCamera.Orthogonal.DistanceTo(GameCamera.LastOrthogonal);
                int idx = Array.IndexOf(list, _currentApproach) + dist;
                idx = Mathf.PosMod(idx, list.Length);
                
                _currentApproach = list[idx];
                RefreshAction(true);
            }
        }

        private void OnRotating(float step)
        {
            if (IsAllowed(Player.Action) && Mathf.IsEqualApprox(step, 0.5f, 0.05f))
            {
                if (Player.Action == ActionType.ClimbBack || Player.Action == ActionType.ClimbBackSideways)
                    Player.InBackground = false;
            }
        }

        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.Teeter:
                case ActionType.IdlePlay:
                case ActionType.IdleSleep:
                case ActionType.IdleLookAround:
                case ActionType.IdleYawn:
                case ActionType.Idle:
                case ActionType.LookUp:
                case ActionType.LookDown:
                case ActionType.LookLeft:
                case ActionType.LookRight:
                case ActionType.Walk:
                case ActionType.Run:
                case ActionType.Jump:
                case ActionType.LiftTrile:
                case ActionType.Fall:
                case ActionType.Bounce:
                case ActionType.Fly:
                case ActionType.DropTrile:
                case ActionType.Slide:
                case ActionType.Land:
                case ActionType.CornerGrab:
                case ActionType.ShimmyBack:
                {
                    Area area = IsOnClimbable(out _currentApproach);

                    if (_currentApproach == Direction.None || Player.ChangeArea is not null ||
                        (!FzInput.IsPressed(InputAction.Up) || Player.Action.IsOnLedge()) &&
                        (Player.OnFloor || _currentApproach != Direction.Left && _currentApproach != Direction.Right ||
                         Mathf.Sign(FzInput.Movement.x) != _currentApproach.Sign()) &&
                        (!Player.Action.IsOnLedge() || !FzInput.AnyPressed(InputAction.Down)))
                        break;

                    Player.HeldBody = area;
                    Player.NextAction = _currentApproach switch
                    {
                        Direction.Right => ActionType.ClimbSide,
                        Direction.Left => ActionType.ClimbSide,
                        Direction.Backward => ActionType.ClimbBack,
                        Direction.Forward => ActionType.ClimbFront,
                        _ => ActionType.None,
                    };
                    _currentLayer = Player.HeldBody.GetLayer() & ~PhysicsLayer.Background;

                    if (_currentLayer == PhysicsLayer.Ladder)
                        PrepareLadderTransition();
                    else
                        PrepareVineTransition();

                    if (_currentApproach == Direction.Left || _currentApproach == Direction.Right)
                        Player.FacingDirection = _currentApproach;
                    break;
                }
            }
        }

        private void PrepareVineTransition()
        {
            if (Player.Action.IsOnLedge())
            {
                Player.Action = Player.NextAction;
                Player.NextAction = ActionType.None;
                return;
            }

            Player.Velocity = Vector3.Zero;
            Player.Action = _currentApproach == Direction.Backward
                ? ActionType.JumpToClimb
                : ActionType.JumpToClimbSide;
        }

        private void PrepareLadderTransition()
        {
            if (Player.OnFloor)
            {
                ActionType action = _currentApproach switch
                {
                    Direction.Backward => ActionType.IdleToClimbBack,
                    Direction.Forward => ActionType.IdleToClimbFront,
                    Direction.Left => ActionType.IdleToClimbSide,
                    Direction.Right => ActionType.IdleToClimbSide,
                    _ => ActionType.None,
                };

                Transform transform = Player.GlobalTransform;
                transform.origin = GetDestination();
                bool empty = SpaceState.CastPoint(transform, PhysicsLayer.Ladder) is null;

                if (!empty)
                {
                    WalkTo.NextOrigin = GetDestination;
                    WalkTo.NextAction = action;
                    Player.Action = ActionType.WalkTo;
                }
                else
                {
                    Player.Action = action;
                    Player.Origin -= OffsetY * Player.Axes.Up;
                }
            }
            else
            {
                Player.Action = _currentApproach == Direction.Backward
                    ? ActionType.JumpToClimb
                    : ActionType.JumpToClimbSide;
            }
        }

        protected override void OnEnter()
        {
            AudioPlayer.PitchScale = 1f;
            AudioPlayer.VolumeDb = GD.Linear2Db(1f);

            Player.Origin = Player.Origin * Player.Axes.XyMask +
                            Player.HeldBody.GlobalTransform.origin * Player.Axes.ZMask;

            if (_currentLayer == PhysicsLayer.Ladder)
            {
                AudioPlayer.Stream = ClimbLadderSound;
                Signals.EmitSignal(nameof(ActionSignals.ClimbedLadder));
            }
            else
            {
                AudioPlayer.Stream = ClimbVineSound;
                Signals.EmitSignal(nameof(ActionSignals.ClimbedVine));
            }
        }

        protected override void OnEnd()
        {
            AudioPlayer.PitchScale = 1f;
            AudioPlayer.VolumeDb = GD.Linear2Db(1f);
        }

        protected override void OnAct(float delta)
        {
            Area area = IsOnClimbable(out Direction approach, _currentLayer);
            Player.HeldBody = area;
            if (area is null || approach == Direction.None)
            {
                Player.Action = ActionType.Idle;
                return;
            }

            RefreshAction();
            RefreshDirection();

            if (_currentLayer == PhysicsLayer.Ladder)
                ActClimbLadder(area);
            else
                ActClimbVine(area, approach);

            UpdateAnimations();
        }

        private void ActClimbVine(Area area, Direction approach)
        {
            if ((_currentApproach == Direction.Backward || _currentApproach == Direction.Forward) &&
                (approach == Direction.Right || approach == Direction.Left))
                _currentApproach = approach;

            if (Player.Action == ActionType.ClimbSide && Mathf.Abs(FzInput.Movement.x) > 0.5f)
            {
                Vector3 offset1 = Mathf.Sign(FzInput.Movement.x) * Player.Axes.Right;

                Transform transform = Player.GlobalTransform;
                transform.origin = Player.Origin + offset1;
                Area area1 = SpaceState.CastRect<Area>(transform, Player.Size, PhysicsLayer.Vine).First;

                if (area1 is not null)
                {
                    Player.Origin += offset1;
                    Player.HeldBody = IsOnClimbable(out _currentApproach, _currentLayer);
                }
            }

            if (area is null || _currentApproach == Direction.None)
            {
                Player.Action = ActionType.Idle;
                return;
            }

            Vector3 mask = Vector3.Zero;
            switch (_currentApproach)
            {
                case Direction.Right:
                case Direction.Left:
                    CollisionObject obj = SpaceState.CastRect(Player.GlobalTransform, Player.Size, PhysicsLayer.Vine).First;
                    mask = obj is null ? Player.Axes.XMask : Player.Axes.XzMask;
                    break;

                case Direction.Backward:
                case Direction.Forward:
                    mask = Player.Axes.ZMask;
                    break;
            }

            Player.Origin = Player.Origin * (Vector3.One - mask) +
                            (area.GlobalTransform.origin) * mask;

            Vector3 velocity = (FzInput.Movement * Player.DefaultSpeed * VineClimbFactor).ToVector3();
            if (Player.Action != ActionType.ClimbSide)
                velocity *= 0.75f;

            Player.Velocity = CheckNextClimbable(velocity, out bool topReached);
            if (topReached)
            {
                switch (_currentApproach)
                {
                    case Direction.Backward:
                    case Direction.Forward:
                        Player.HeldBody = null;
                        Player.Origin += Vector3.Up / 2f;
                        Player.Action = ActionType.Fall;
                        break;

                    case Direction.Left:
                    case Direction.Right:
                        Transform transform = Player.GlobalTransform;
                        transform.origin += Player.Axes.Facing * Player.Size.x;

                        CollisionObject floor = SpaceState.CastRect(transform, Player.Size, PhysicsLayer.TopOnly).First;

                        Player.HeldBody = floor;
                        Player.Action = ActionType.CornerGrab;
                        Player.Velocity = Vector3.Zero;

                        Vector3 size = floor.ShapeOwnerGetXFormExtents(0, Player.Transform.basis);
                        Vector3 offset = Player.Axes.Up * size -
                                         Player.Axes.Facing * (size + Player.Size);
                        Player.Origin = floor.GlobalTransform.origin + offset;
                        break;
                }
            }
        }

        private void ActClimbLadder(Area area)
        {
            Player.Origin = Player.Origin * Player.Axes.YMask +
                            area.GlobalTransform.origin * Player.Axes.XzMask;

            Vector3 velocity = FzInput.Movement.y * Player.DefaultSpeed * LadderClimbFactor * Vector3.Up;

            Player.Velocity = CheckNextClimbable(velocity, out bool topReached);
            if (topReached)
            {
                switch (_currentApproach)
                {
                    case Direction.Right:
                    case Direction.Left:
                        if (FzInput.AnyPressed(InputAction.Left) || FzInput.AnyPressed(InputAction.Right))
                            Player.Action = ActionType.ClimbOver;
                        break;

                    case Direction.Backward:
                    case Direction.Forward:
                        Player.HeldBody = null;
                        Player.Action = ActionType.Fall;
                        Player.Origin += Vector3.Up / 2f;
                        break;
                }
            }
        }

        private void UpdateAnimations()
        {
            Animation anim = AnimationPlayer.GetAnimation(Player.Action.GetAnimationName());
            float length = Player.Velocity.Normalized().Length();
            if (!Mathf.IsZeroApprox(length))
            {
                anim.Loop = true;
                if (!AnimationPlayer.IsPlaying())
                {
                    AudioPlayer.Play();
                    AnimationPlayer.Play(AnimationPlayer.AssignedAnimation, -1f, Mathf.Abs(length));
                }
            }
            else
            {
                anim.Loop = false;
                if (AnimationPlayer.IsPlaying())
                {
                    AudioPlayer.Stop();
                    AnimationPlayer.Seek(0, true);
                }
            }
        }

        private Area IsOnClimbable(out Direction approach, PhysicsLayer layer = PhysicsLayer.None)
        {
            PhysicsLayer mask = PhysicsLayer.Climbable;
            if (layer != PhysicsLayer.None)
                mask = layer;

            var pair = SpaceState.CastRect(Player.GlobalTransform, Player.Size, mask);
            
            _onSingleClimbable = pair.Far is null;
            approach = Direction.None;

            if (pair.Near is Area area)
            {
                Orthogonal gomezView = Player.GlobalTransform.basis.GetOrthogonal();
                Orthogonal areaView = area.GlobalTransform.basis.GetOrthogonal();

                if (area.GetLayer() == PhysicsLayer.Ladder)
                    areaView = areaView.Rotated(1);
                int distance = gomezView.DistanceTo(areaView);
                
                approach = distance switch
                {
                    0 => Direction.Backward,
                    +1 => Direction.Left,
                    -1 => Direction.Right,
                    _ => Player.InBackground ? Direction.Forward : Direction.None
                };
                return area;
            }
            return null;
        }

        private Vector3 GetDestination()
        {
            return Player.Origin * Player.Axes.YMask +
                   Player.HeldBody.GlobalTransform.origin * Player.Axes.XzMask;
        }

        private Vector3 CheckNextClimbable(Vector3 velocity, out bool topReached)
        {
            topReached = false;
            if (velocity.LengthSquared() <= 0f)
                return Vector3.Zero;

            Transform transform = Player.HeldBody.ShapeOwnerGetTransform(0);
            Vector3 extents = Player.HeldBody.ShapeOwnerGetExtents(0);

            Vector3 sign = velocity.Sign();
            Transform check = Player.GlobalTransform;
            check.origin += (sign * Vector3.Right) * Player.Size.x;
            check.origin += (sign * Vector3.Up) * Player.Size.y;

            CollisionObject next = SpaceState.CastPoint(check, PhysicsLayer.Climbable);
            PhysicsLayer nextLayer = next.GetLayer();

            if ((nextLayer & PhysicsLayer.Climbable) != 0)
                return velocity;

            float playerY = Player.Origin.Dot(Player.Axes.Up);
            float heldY = transform.origin.Dot(transform.basis.y);

            if (!Mathf.IsZeroApprox(sign.y) && Mathf.Abs(heldY - playerY) > extents.y && _onSingleClimbable)
            {
                topReached = sign.y > 0f;
                velocity.y = 0f;
            }

            float playerX = Player.Origin.Dot(Player.Axes.Right);
            float heldX = transform.origin.Dot(transform.basis.x);

            if (!Mathf.IsZeroApprox(sign.x) && Mathf.Abs(heldX - playerX) > extents.x &&
                nextLayer != PhysicsLayer.Vine && _onSingleClimbable)
            {
                velocity.x = 0f;
            }

            return velocity;
        }

        private void RefreshAction(bool force = false)
        {
            if (force || !FzInput.Movement.IsZeroApprox())
            {
                switch (_currentApproach)
                {
                    case Direction.Right:
                    case Direction.Left:
                        Player.Action = ActionType.ClimbSide;
                        break;
                    case Direction.Backward:
                        Player.Action = !Mathf.IsZeroApprox(FzInput.Movement.y)
                            ? ActionType.ClimbBack
                            : ActionType.ClimbBackSideways;
                        break;
                    case Direction.Forward:
                        Player.Action = !Mathf.IsZeroApprox(FzInput.Movement.y)
                            ? ActionType.ClimbFront
                            : ActionType.ClimbFrontSideways;
                        break;
                }
            }
        }

        private void RefreshDirection()
        {
            if (Player.Action == ActionType.ClimbSide)
            {
                Player.FacingDirection = _currentApproach;
            }
            else if (!Mathf.IsZeroApprox(FzInput.Movement.x))
            {
                Player.FacingDirection = FzInput.Movement.x.GetDirectionFrom();
            }
        }

        protected override bool IsAllowed(ActionType type) => Player.Action.IsClimbing();
    }
}