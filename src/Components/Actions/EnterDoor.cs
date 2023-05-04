using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class EnterDoor : PlayerAction
    {
        [Export] public readonly float SpinThroughFactor = 0.75f;
        [Export] public readonly float DefaultFactor = 1.25f;

        private bool _hasFlipped;
        private bool _hasChangedLevel;
        private bool _spinThrough;
        private Vector3 _spinOrigin;
        private Vector3 _spinDestination;
        private float _step = -1f;
        private float _transition;

        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.Idle:
                case ActionType.IdlePlay:
                case ActionType.IdleSleep:
                case ActionType.IdleLookAround:
                case ActionType.IdleYawn:
                case ActionType.LookLeft:
                case ActionType.LookRight:
                case ActionType.LookUp:
                case ActionType.LookDown:
                case ActionType.Walk:
                case ActionType.Run:
                case ActionType.DropDown:
                case ActionType.Slide:
                case ActionType.Land:
                case ActionType.Teeter:
                case ActionType.TeeterPaul:
                    if (!Mathf.IsEqualApprox(_step, -1f) || !FzInput.IsJustPressed(InputAction.Up) ||
                        !Player.OnFloor || Player.InBackground ||
                        (Player.ChangeArea is null || Player.ChangeArea.Door != null || 
                         Player.ChangeArea.BitDoor != null || Player.ChangeArea.Warp != null))
                        break;

                    Axes axes = new Axes(Player.ChangeArea!.GlobalTransform.basis);
                    if (!Mathf.IsEqualApprox(axes.Right.Dot(Player.Axes.Right), 1))
                        break;

                    _spinThrough = Player.ChangeArea.DoSpin;
                    if (_spinThrough)
                    {
                        Vector3 size = Player.ChangeArea.ShapeOwnerGetXFormExtents(0, GameCamera.Orthogonal.GetBasis());
                        Vector3 offset = Player.ChangeArea.GlobalTransform.origin -
                                         axes.Forward * (size.z - 1f);

                        if (Player.Origin.Dot(axes.Forward) < offset.Dot(axes.Forward))
                            Player.Origin = (Vector3.One - axes.ZMask) + offset * axes.ZMask;

                        _spinOrigin = GetDestination();
                        _spinDestination = GetDestination() + (size.z * axes.Forward * 1.5f);
                    }

                    if (Player.CarriedBody != null)
                    {
                        Player.Origin = GetDestination();

                        if (!Player.CarriedBody.IsHeavy)
                            Player.Action = (_spinThrough) ? ActionType.CarryEnterDoorSpin : ActionType.CarryEnter;
                        else
                            Player.Action = (_spinThrough)
                                ? ActionType.CarryHeavyEnterDoorSpin
                                : ActionType.CarryHeavyEnter;
                        break;
                    }

                    WalkTo.NextOrigin = GetDestination;
                    WalkTo.NextAction = (_spinThrough) ? ActionType.EnterDoorSpin : ActionType.EnterDoor;
                    Player.Action = ActionType.WalkTo;
                    break;
            }
        }

        private Vector3 GetDestination()
        {
            if (Player.ChangeArea != null)
            {
                Basis basis = GameCamera.Orthogonal.GetBasis();
                return Player.Origin * (basis.y.Abs() + basis.z.Abs()) +
                       Player.ChangeArea.GlobalTransform.origin * basis.x.Abs();
            }

            return Player.Origin;
        }

        protected override void OnEnter()
        {
            if (Player.ChangeArea is null)
            {
                Player.Action = ActionType.Idle;
                return;
            }

            _transition = 0f;
            _step = 0f;
            _hasFlipped = _hasChangedLevel = false;
            if (_spinThrough)
            {
                GameCamera.ChangeRotation(GameCamera.Orthogonal.Rotated(1), 2f);
                Player.FacingDirection = Direction.Right;
            }
            else
            {
                FadeManager.DoSquare(Colors.Black, GameCamera.UnprojectPosition(Player.Origin), 
                    AnimationPlayer.CurrentAnimationLength, false);
            }

            Player.Velocity *= Player.Axes.Up;
            Signals.EmitSignal(nameof(ActionSignals.EnteredDoor));
        }

        protected override void OnAct(float delta)
        {
            if (_step < 1f && _spinThrough && !_hasFlipped)
            {
                Vector3 initial = Player.Origin;
                Player.Origin = _spinOrigin.LinearInterpolate(_spinDestination, _step);
                if (Player.CarriedBody != null)
                {
                    Transform transform = Player.CarriedBody.GlobalTransform;
                    transform.origin += Player.Origin - initial;
                    Player.CarriedBody.GlobalTransform = transform;
                }
            }

            if (string.IsNullOrEmpty(Player.ChangeArea.NextLevel))
            {
                _step = -1f;
                Player.Action = ActionType.Idle;
                return;
            }

            if (_spinThrough && _hasFlipped && !_hasChangedLevel)
            {
                _step = 0f;
            }
            else
            {
                _transition += delta;
                float factor = _spinThrough ? SpinThroughFactor : DefaultFactor;
                if (_hasChangedLevel)
                    factor *= SpinThroughFactor;
                _step = _transition / factor;
            }

            if (_step >= 1f && !_hasFlipped)
            {
                _transition = 0f;
                _step = 0f;
                _hasFlipped = true;
            }
            else if (_step >= 1f && _hasFlipped)
            {
                _step = -1f;
                _spinThrough = false;
                
                Player.NextAction = Player.Action switch
                {
                    ActionType.EnterDoor => ActionType.ExitDoor,
                    ActionType.CarryEnter => ActionType.CarryExit,
                    ActionType.CarryHeavyEnter => ActionType.CarryExit,
                    _ => ActionType.None
                };
                Player.Action = ActionType.None;
            }

            if (_spinThrough && _hasFlipped && !GameCamera.Tween.IsActive() && !_hasChangedLevel)
            {
                _hasChangedLevel = true;
                GameCamera.ChangeRotation(GameCamera.Orthogonal.Rotated(1));
            }
        }

        protected override void OnEnd()
        {
            GameState.ChangeLevel(Player.ChangeArea.NextLevel, Player.ChangeArea.VolumeId);
        }

        protected override bool IsAllowed(ActionType type) =>
            type.In(
                ActionType.EnterDoor,
                ActionType.CarryEnter,
                ActionType.CarryHeavyEnter,
                ActionType.EnterDoorSpin,
                ActionType.CarryEnterDoorSpin,
                ActionType.CarryHeavyEnterDoorSpin);
    }
}