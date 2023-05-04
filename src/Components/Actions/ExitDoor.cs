using Godot;
using Zeffyr.Structure;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class ExitDoor : PlayerAction
    {
        protected override void OnEnter()
        {
            Vector2 position = GetViewport().Size / 2f;
            FadeManager.DoSquare(Colors.Black, position, AnimationPlayer.CurrentAnimationLength, true);
        }

        protected override void OnAct(float delta)
        {
            if (!AnimationPlayer.IsPlaying())
                Player.Action = ActionType.Idle;
        }

        protected override bool IsAllowed(ActionType type) =>
            type.In(ActionType.ExitDoor, ActionType.CarryExit, ActionType.CarryHeavyExit);
    }
}