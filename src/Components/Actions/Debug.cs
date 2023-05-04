using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class Debug : PlayerAction
    {
        protected override void OnAct(float delta)
        {
            Player.Action = ActionType.Fall;
            Player.Velocity = FzInput.Movement.ToVector3() * Player.DefaultSpeed;
        }

        protected override bool IsAllowed(ActionType type) => OS.IsDebugBuild() && Debug.FreeMode;
    }
}