using Godot;
using System.Collections.Generic;
using Zeffyr.Managers;
using Zeffyr.Structure;

namespace Zeffyr.Components.Map
{
    // TODO: make it work!
    public class WorldMap : Spatial
    {
        [RootNode] public GameStateManager GameState;

        [RootNode] public TimeManager TimeManager;
        
        [RootNode] public FadeManager FadeManager;

        [Node] public MapCamera MapCamera;

        [Node] public Spatial MapTree;

        public override void _Ready()
        {
            this.InjectNodes();
            TimeManager.SetProcess(false);

            IEnumerable<MapNode> nodes = MapTree.GetChildrenRecursive<MapNode>();
            foreach (MapNode node in nodes)
            {
                if (GameState.SaveData.World.TryGetValue(node.Name, out LevelSaveData levelSave))
                {
                    WinConditions filled = levelSave.FilledConditions;
                    node.IsActive = GameState.SaveData.Level.Contains(node.Name.ToLower());

                    node.State = filled.Fills(node.Conditions)
                        ? MapNode.MapState.Completed
                        : MapNode.MapState.Discovered;
                    node.ShowProgress(filled);
                }
                else
                {
                    MapNode parent = node.GetParentOrNull<MapNode>();
                    if (parent is not null)
                    {
                        node.Visible = (parent.State != MapNode.MapState.Unknown);
                        node.State = MapNode.MapState.Unknown;
                    }
                }
            }
        }

        public override void _ExitTree()
        {
            TimeManager.SetProcess(true);
        }
    }
}
