using Zeffyr.Structure;
using Zeffyr.Structure.Input;

namespace Zeffyr.Components.Actions
{
    public class ReadListen : PlayerAction
    {
        public static SpeechBubble SpeechInstance { get; set; }

        private float _sinceActive;
        
        protected override void OnEnter()
        {
            _sinceActive = 0f;
            Signals.EmitSignal(nameof(ActionSignals.ReadHeard));
        }

        protected override void OnAct(float delta)
        {
            _sinceActive += delta;
            if (FzInput.IsPressed(InputAction.TalkCancel) && IsInstanceValid(SpeechInstance) && _sinceActive >= 0.25f)
            {
                SpeechInstance.OnEnd();
                Player.Action = ActionType.Idle;
            }
        }

        protected override bool IsAllowed(ActionType type) => type == ActionType.ReadListen;
    }
}