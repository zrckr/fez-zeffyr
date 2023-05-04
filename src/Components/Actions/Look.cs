using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class Look : PlayerAction
    {
        private const float Offset = 0.4f;
        private ActionType _nextAction;

        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.LookUp:
                case ActionType.LookDown:
                case ActionType.LookRight:
                case ActionType.LookLeft:

                case ActionType.Idle:
                case ActionType.IdlePlay:
                case ActionType.IdleSleep:
                case ActionType.IdleLookAround:
                case ActionType.IdleYawn:
                case ActionType.Teeter:
                    Vector2 v = FzInput.FreeLook;
                        if (v.y > Offset)
                            _nextAction = ActionType.LookUp;
                        else if (v.y < -Offset)
                            _nextAction = ActionType.LookDown;
                        else if (v.x < -Offset)
                            _nextAction = ActionType.LookLeft;
                        else if (v.x > Offset)
                            _nextAction = ActionType.LookRight;

                        else if (FzInput.FreeLook.IsZeroApprox())
                            _nextAction = ActionType.Idle;

                    if (Player.FacingDirection == Direction.Left && (_nextAction.In(ActionType.LookLeft, ActionType.LookLeft)))
                        _nextAction = (_nextAction == ActionType.LookRight)
                            ? ActionType.LookLeft
                            : ActionType.LookRight;

                    if (Player.Action.IsIdle() && _nextAction != ActionType.None && _nextAction != ActionType.Idle)
                    {
                        Player.Action = _nextAction;
                        _nextAction = ActionType.None;
                    }
                    
                    if (_nextAction != Player.Action)
                        break;
                    
                    _nextAction = ActionType.None;
                    break;
                
                default:
                    _nextAction = ActionType.None;
                    break;
            }
        }

        protected override void OnEnter()
        {
            Signals.EmitSignal(nameof(ActionSignals.LookedAround));
        }

        protected override void OnAct(float delta)
        {
            if (AnimationPlayer.CurrentAnimationPosition > AnimationPlayer.CurrentAnimationLength / 2f)
                AnimationPlayer.PlaybackSpeed = 0f;

            if (!Mathf.IsEqualApprox(AnimationPlayer.CurrentAnimationPosition, AnimationPlayer.CurrentAnimationLength) && _nextAction != ActionType.None)
                AnimationPlayer.PlaybackSpeed = 1.25f;

            if (!AnimationPlayer.IsPlaying() && _nextAction != ActionType.None)
            {
                this.Player.Action = _nextAction;
                _nextAction = ActionType.None;
            }
        }

        protected override bool IsAllowed(ActionType type) => 
            type.In(ActionType.LookUp, ActionType.LookDown, ActionType.LookLeft, ActionType.LookRight);
    }
}