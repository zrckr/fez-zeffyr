using Godot;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Zeffyr.Structure;
using Zeffyr.Managers;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components
{
    [DebuggerDisplay("{Name}")]
    public class Level : Spatial
    {
        [Export] public readonly AudioStream Song;

        [Node] public GridMap Triles;

        [Node] public Gomez Gomez;

        [Node] public GameCamera GameCamera;

        [Node] public DirectionalLight Sun;

        [Node("Sun/Sun2", true)] public DirectionalLight Sun2;

        [Node(true)] public Water Water;

        [RootNode] protected TimeManager TimeManager;

        [RootNode] protected GameStateManager GameState;

        [RootNode] protected MusicManager MusicManager;

        [RootNode] protected DebugManager DebugManager;

        [RootNode] protected FadeManager FadeManager;

        private readonly AudioStreamSample _secretSolved =
            ResourceLoader.Load<AudioStreamSample>("res://assets/Sounds/Miscactors/secretsolved.wav");

        protected LevelSaveData State => GameState.SaveData.ThisLevel;

        public override void _Ready()
        {
            this.InjectNodes();
            this.ReadSaveData();
            this.InitFromEditor();
            this.ProcessLandmark();

            // Remove inactive nodes
            foreach (string nodePath in State.InactiveNodes)
            {
                Node node = GetNode(nodePath);
                if (IsInstanceValid(node))
                    node.QueueFree();
            }

            // Regenerate collisions for GridMaps
            foreach (GridMap map in this.GetChildrenRecursive<GridMap>())
            {
                GenerateCollisions(map);
            }

            MusicManager.PlayNewSong(Song, 1f);
            FadeManager.DoSquare(Colors.Black, GetViewport().Size / 2f, 1f, true);
            Water?.ReestablishHeight(Gomez);
        }

        public override void _Process(float delta)
        {
            if (Sun is not null)
                Sun.LightEnergy = Sun2.LightEnergy = 1.25f - TimeManager.NightTransition;
        }

        public void ResolvePuzzle(Area area = null)
        {
            GameState.MakeNodeInactive(area);
            ResolvePuzzleSilent();
            ResolvePuzzleSoundOnly();
        }

        public void ResolvePuzzleSilent()
        {
            State.FilledConditions.Secrets += 1;
            GameState.SaveGame();
        }

        public void ResolvePuzzleSoundOnly()
        {
            AudioStreamPlayer player = new AudioStreamPlayer()
            {
                Bus = "Sounds",
                Stream = _secretSolved,
            };
            AddChild(player);
            player.Play();

            MusicManager.MusicVolume = 0.125f;
            Task.Delay(2750).ContinueWith(_ =>
            {
                player.QueueFree();
                MusicManager.FadeVolume(0.125f, 1f, 3f);
            });
        }

        private void InitFromEditor()
        {
            if (!FzInput.DevicesInitialized)
            {
                FzInput.LoadDevices(this);
                FzInput.LoadCustomCursors();
            }

            if (SettingsHelper.Settings is null)
            {
                SettingsHelper.Load();
                SettingsHelper.Apply(this);
            }

            if (!TimeManager.IsProcessing())
            {
                TimeManager.Reset();
            }
        }

        private void ProcessLandmark()
        {
            if (GameState.NextChangeInfo.HasValue)
            {
                var info = GameState.NextChangeInfo.Value;
                Spatial spawn = GetNode<Spatial>("Volumes")
                    .FindNode(info.Volume.ToString(), true, false) as Spatial;

                if (spawn is null)
                    throw new NullReferenceException($"Volume `{info.Volume}` not found on the level.");

                if (info.Action != ActionType.None)
                {
                    Gomez.Origin = spawn.GlobalTransform.origin;
                    Gomez.Action = info.Action;
                }

                if (info.Pickup != null)
                {
                    Gomez.AddChild(info.Pickup);
                    Gomez.CarriedBody = info.Pickup;
                }

                GameState.NextChangeInfo = null;
            }
        }

        private void ReadSaveData()
        {
            string name = Filename;
            GameState.InGame = true;
            GameState.SaveData.Level = name;
            if (GameState.SaveData.World.TryGetValue(name, out LevelSaveData saveData))
            {
                saveData.FirstVisit = false;
            }
            else
            {
                GameState.SaveData.World.Add(name, new LevelSaveData()
                {
                    FirstVisit = true
                });
            }
        }

        private void GenerateCollisions(GridMap gridMap)
        {
            string path = System.IO.Path.ChangeExtension(gridMap.MeshLibrary.ResourcePath, ".tscn");
            Node trileset = ResourceLoader.Load<PackedScene>(path).Instance();
            int count = trileset.GetChildCount();

            gridMap.CollisionLayer = gridMap.CollisionMask = 0;
            Vector3[] cells = gridMap.GetUsedCells().Cast<Vector3>().ToArray();

            for (int i = 0; i < cells.Length; i++)
            {
                int x = (int) cells[i].x;
                int y = (int) cells[i].y;
                int z = (int) cells[i].z;

                int id = gridMap.GetCellItem(x, y, z);
                int rot = gridMap.GetCellItemOrientation(x, y, z);
                Basis basis = Mathz.OrthogonalBases[rot];

                string name = trileset.GetChild(id).Name;
                Trile trile = trileset.GetChildOrNull<Trile>(id);

                if (trile is null)
                    throw new ArgumentException($"Node {name} is not a Trile!");
                else
                    name = $"[{i}] {name} - ";

                if (trile.Layer != PhysicsLayer.None)
                {
                    bool disableShape = false;
                    CollisionObject collisionObject;
                    Shape shape = new BoxShape()
                    {
                        Extents = trile.Size / 2f
                    };
                    
                    if ((trile.Layer & PhysicsLayer.Climbable) != 0)
                    {
                        collisionObject = new Area()
                        {
                            Name = name + nameof(Area),
                            CollisionLayer = (uint) trile.Layer,
                            CollisionMask = (uint) trile.Mask,
                            Monitorable = true,
                            Monitoring = true,
                        };
                    }
                    else
                    {
                        disableShape = trile.Layer.HasFlag(PhysicsLayer.Background);
                        collisionObject = new StaticBody()
                        {
                            Name = name + nameof(StaticBody),
                            CollisionLayer = (uint) trile.Layer,
                            CollisionMask = (uint) trile.Mask,
                        };
                    }

                    // Gomez.cs uses groups for distinctions in surface type
                    foreach (SurfaceType surface in Enum.GetValues(typeof(SurfaceType)))
                    {
                        if (trile.SurfaceType == surface)
                        {
                            collisionObject.AddToGroup(surface.ToString());
                            break;
                        }
                    }
                    // Generate actual collision shape
                    CollisionShape collision = new CollisionShape
                    {
                        Shape = shape, Disabled = disableShape
                    };
                    collisionObject.CallDeferred("add_child", collision);
                    gridMap.CallDeferred("add_child", collisionObject);
                    collisionObject.SetDeferred("transform", new Transform(basis, cells[i]));
                }

                if (trile.CollisionOnly)
                {
                    /*
                     * Due to an error in `grid_map.cpp` some of the deleted cells using the INVALID_CELL_ITEM id
                     * have a null reference for static body. Instead of erasing, we create
                     * new empty cells with a non-existent id in their place.
                     */
                    gridMap.SetCellItem(x, y, z, count, rot);
                }
            }

            trileset.QueueFree();
        }
    }
}