using Godot;
using System.Collections.Generic;
using System.Linq;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Map
{
    [Tool]
    public class MapNode : Spatial
    {
        public enum MapState { Unknown, Discovered, Completed }

        [Export]
        public bool IsActive;

        [Export]
        public MapState State = MapState.Discovered;

        [Export]
        public readonly bool HasLesserGate;
        
        [Export]
        public readonly bool HasWarpGate;

        [Export]
        public readonly float NodeSize = 1f;
        
        [Export]
        public readonly float NodeThickness = 0.108f;
        
        [Export]
        public readonly float LinesThickness = 0.054f;
       
        [Export]
        public readonly Texture MapScreen;

        [Export]
        public readonly Color DefaultColor = Colors.White;
        
        [Export]
        public readonly Color UnknownColor = Colors.WebGray;
        
        [Export]
        public readonly Color CompletedColor = new Color(1.00f, 0.74f, 0.14f);
        
        [Export]
        public readonly Color LinesColor = Colors.White;

        [Export]
        private Dictionary<string, int> _conditions = new Dictionary<string, int>()
        {
            ["Unlocked Doors"] = 0,
            ["Locked Doors"] = 0,
            ["Chests"] = 0,
            ["Big Cubes"] = 0,
            ["Other"] = 0,
            ["Small Cubes"] = 0,
            ["Secrets"] = 0,
        };

        public WinConditions Conditions => new WinConditions()
        {
            UnlockedDoors = _conditions["Unlocked Doors"],
            LockedDoors = _conditions["Locked Doors"],
            Chests = _conditions["Chests"],
            BigCubes = _conditions["Big Cubes"],
            OtherCollectibles = _conditions["Other"],
            SmallCubes = _conditions["Small Cubes"],
            Secrets = _conditions["Secrets"],
        };

        [Node] protected Particles Waves;
        [Node] protected Spatial Icons;
        [Node] protected Tween Tween;
        [Node] protected MeshInstance Mesh;
        [Node] protected MeshInstance Outline;
        [Node] protected Area Area;

        private ShaderMaterial MeshMaterial => (ShaderMaterial) Mesh.GetSurfaceMaterial(0);
        private SpatialMaterial OutlineMaterial => (SpatialMaterial) Outline.GetSurfaceMaterial(0);
        private ParticlesMaterial WavesMaterial => (ParticlesMaterial) Waves.ProcessMaterial;
        private CubeMesh MainMesh => (CubeMesh) Mesh.Mesh;
        private CubeMesh OutlineMesh => (CubeMesh) Outline.Mesh;

        private readonly SpatialMaterial _linesMaterial = new SpatialMaterial();

        public override void _Ready()
        {
            this.InjectNodes();
            this.AddConnections();

            Color meshModulate = default;
            Texture mapScreen = default;
            Color outlineAlbedo = default;
            
            switch (State)
            {
                case MapState.Unknown:
                    meshModulate = UnknownColor;
                    outlineAlbedo = DefaultColor;
                    break;
                case MapState.Discovered:
                    mapScreen = MapScreen;
                    meshModulate = DefaultColor;
                    outlineAlbedo = DefaultColor;
                    break;
                case MapState.Completed:
                    mapScreen = MapScreen;
                    meshModulate = CompletedColor;
                    outlineAlbedo = CompletedColor;
                    break;
            }
            
            MeshMaterial.SetShaderParam("modulate", meshModulate);
            MeshMaterial.SetShaderParam("map_screen", mapScreen);
            OutlineMaterial.AlbedoColor = outlineAlbedo;
            
            MainMesh.Size = Vector3.One * NodeSize;
            OutlineMesh.Size = Vector3.One * (NodeSize + NodeThickness);
            OutlineMesh.Size = Vector3.One * (NodeSize + NodeThickness);
            
            Area.ShapeOwnerSetExtents(0, Vector3.One * NodeSize / 2f);
            WavesMaterial.Scale = 3f * NodeSize;

            float xz = (NodeSize + NodeThickness) / 2f + 0.25f;
            float y = NodeSize / 2f;
            Icons.Translation = new Vector3(xz, y, xz);
            Waves.Emitting = IsActive;
        }

        public void Pop(bool backwards)
        {
            Vector3 initial = Vector3.One * (NodeSize + NodeThickness);
            Vector3 final = Vector3.One * (NodeSize + NodeThickness * 4f);
            Tween.EaseType easing = Tween.EaseType.In;
            
            if (backwards)
            {
                (initial, final) = (final, initial);
                easing = Tween.EaseType.Out;
            }

            Tween.StopAll();
            Tween.InterpolateProperty(Outline.Mesh, "size", initial, final, 0.33f,
                Tween.TransitionType.Sine, easing);
            Tween.Start();
        }

        public void ShowProgress(WinConditions filled)
        {
            int visible = 0;
            foreach (Spatial icon in Icons.GetChildren())
            {
                icon.Visible = icon.Name switch
                {
                    "Warp" => HasWarpGate,
                    "Lesser" => HasLesserGate,
                    "Treasure" => filled.OtherCollectibles < Conditions.OtherCollectibles,
                    "Locked" => filled.LockedDoors < Conditions.LockedDoors,
                    "Big" => filled.BigCubes < Conditions.BigCubes,
                    "Small" => filled.SmallCubes < Conditions.SmallCubes,
                    "Secret" => filled.Secrets < Conditions.Secrets,
                    _ => false,
                };

                if (icon.Visible)
                {
                    icon.Translation = Vector3.Up * visible * -0.5f;
                    visible += 1;
                }
            }
        }

        private void AddConnections()
        {
            _linesMaterial.VertexColorUseAsAlbedo = true;
            _linesMaterial.AlbedoColor = LinesColor;
            _linesMaterial.ParamsCullMode = SpatialMaterial.CullMode.Disabled;

            int i = 0;
            MapNode[] children = GetChildren().OfType<MapNode>().ToArray();
            foreach (MapNode child in children)
            {
                LineRenderer line = new LineRenderer()
                {
                    Points = new[] {this.GlobalTransform.origin, child.GlobalTransform.origin},
                    StartThickness = LinesThickness,
                    EndThickness = LinesThickness,
                    Visible = this.Visible && child.Visible,
                    MaterialOverride = _linesMaterial,
                    Name = i.ToString(),
                };

                i += 1;
                this.AddChild(line);
                line.SetProcess(Visible);
                line.GlobalTransform = new Transform()
                {
                    basis = line.GlobalTransform.basis,
                    origin = this.GlobalTransform.origin,
                };
            }
        }
    }
}
