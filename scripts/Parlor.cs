using Godot;
using Zeffyr.Components;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;

namespace Zeffyr.Scripts
{
    public class Parlor : Level
    {
        private Area _rotationSwitch;
        private Spatial _currTarget;
        private Spatial _prevTarget;

        public override void _Ready()
        {
            base._Ready();
            _rotationSwitch = GetNode<Area>("Volumes/1");
            _currTarget = GetNode<Spatial>("Volumes/Room");
            _prevTarget = GetNode<Spatial>("Volumes/Secret");
        }

        public override void _Process(float delta)
        {
            if (_rotationSwitch.OverlapsBody(Gomez) && FzInput.IsJustPressed(InputAction.Up))
            {
                (_currTarget, _prevTarget) = (_prevTarget, _currTarget);
                GameCamera.TargetPath = _currTarget.GetPath();
                
                Orthogonal opposite = GameCamera.Orthogonal.Rotated(2);
                GameCamera.ChangeRotation(opposite, 1.5f);
            }
        }
    }
}