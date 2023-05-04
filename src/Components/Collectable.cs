using Godot;
using Zeffyr.Managers;
using Zeffyr.Structure;

namespace Zeffyr.Components
{
    public class Collectable : Area
    {
        public enum Type
        {
            None,
            SmallCube,
            BigCube,
            AntiCube,
            GoldenKey,
            TreasureChest,
        }

        [Export] public Type ItemType { get; private set; }
        
        [Export] public PackedScene TreasureItem { get; private set; }

        [Export]
        public bool Enable
        {
            get => _enable;
            set => Visible = Monitorable = Monitoring = _enable = value;
        }

        [RootNode] public GameStateManager GameState;

        private bool _enable = true;
        
        public override void _Ready()
        {
            this.InjectNodes();
            this.ShapeOwnerSetDisabled(0, !Enable);

            AnimationPlayer player = GetNodeOrNull<AnimationPlayer>("Animation");
            if (player != null)
            {
                switch (ItemType)
                {
                    case Type.TreasureChest:
                        string path = GetPath();
                        GameState.SaveData.World.TryGetValue(Owner.Name, out LevelSaveData saveData);
                        if (saveData != null && saveData.InactiveNodes.Contains(path))
                        {
                            saveData.InactiveNodes.Remove(path);
                            player.Play("default");
                            player.Advance(player.CurrentAnimationLength);
                        }
                        break;
                    default:
                        player.Play("default");
                        break;
                }
            }
        }
    }
}