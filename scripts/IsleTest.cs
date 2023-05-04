using Godot;
using Zeffyr.Components;

namespace Zeffyr.Scripts
{
    public class IsleTest : Level
    {
        public override void _Ready()
        {
            base._Ready();
            CodeArea code = GetNodeOrNull<CodeArea>("CodeArea");
            code?.Connect(nameof(CodeArea.CodeAccepted), this, nameof(OnCodeAccepted));
            
            GetNodeOrNull("RotationTest")
                .GetChildrenList<Spatial>()
                .ForEach(spatial => GD.PrintS(spatial.Name, spatial.GlobalTransform.basis.GetOrthogonalIndex()));
        }

        private void OnCodeAccepted()
        {
            GD.Print($"Accepted!");
        }
    }
}