using Godot;
using Zeffyr.Structure;

namespace Zeffyr.Components.Actions
{
    public class Crush : PlayerAction
    {
        [Export] public float Duration = 1.75f;
        [Export] public float HorzFactor = 1.2f;
        [Export] public float VertFactor = 1f;
        
        private float _elapsed;
        private Vector3 _origin;
        
        protected override void OnEnter()
        {
            Player.Velocity = Vector3.Zero;
            _origin = Player.Origin;
            _elapsed = 0.0f;
        }

        protected override void OnAct(float delta)
        {
            Player.Origin = _origin;
            AnimationPlayer.PlaybackSpeed = 2f;
            
            _elapsed += delta;
            float factor = (Player.Action == ActionType.CrushHorz ? HorzFactor : VertFactor);
            
            if (_elapsed > Duration * factor)
                Player.Respawn.Load();
        }
        
        protected override bool IsAllowed(ActionType type)
            => type == ActionType.CrushVert || type == ActionType.CrushHorz;
    }
}