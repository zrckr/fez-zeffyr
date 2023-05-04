using Zeffyr.Components;

namespace Zeffyr.Scripts
{
    public class AbandonedC : Level
    {
        private CodeArea _codeArea;
        private Collectable _antiCube;

        public override void _Ready()
        {
            base._Ready();
            _antiCube = GetNode<Collectable>("Items/AntiCube");
            _codeArea = GetNode<CodeArea>("Volumes/CodeArea");

            _antiCube.Enable = false;
            _codeArea.Connect(nameof(CodeArea.CodeAccepted), this, nameof(OnCodeAccepted));
        }

        private void OnCodeAccepted()
        {
            _antiCube.Enable = true;
            GameState.MakeNodeInactive(_codeArea, true);
            ResolvePuzzle();
        }
    }
}