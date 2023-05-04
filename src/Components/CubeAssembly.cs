using Godot;
using Zeffyr.Managers;

namespace Zeffyr.Components
{
    public class CubeAssembly : Spatial
    {
        [RootNode] public GameStateManager GameState;

        [Node] public AudioStreamPlayer Audio;

        [Node] public Spatial BigCube;

        [Node] public Spatial Pivot;

        [Node] public Timer Timer;

        [Node("Pivot/Offsets")] public Spatial Offsets;

        [Node("Pivot/Collected")] public Spatial Collected;

        private Spatial[] CollectedCubes 
            => Collected.GetChildrenList<Spatial>().ToArray();

        private AudioStreamSample[] _collectSounds;

        private readonly string[] _shardNotes = new string[]
        {
            "c2", "csharp2", "d2", "dsharp2",
            "e2", "f2", "fsharp2", "g2",
            "gsharp2", "a2", "asharp2", "b2",
            "c3", "csharp3", "d3", "dsharp3",
            "e3", "f3", "fsharp3", "g3",
            "gsharp3", "a3", "asharp3", "b3", "c4",
        };
        
        private bool ShowAssembled
        {
            set
            {
                Pivot.Visible = !value;
                BigCube.Visible = value;
            }
        }

        public override void _Ready()
        {
            this.InjectNodes();
            this.InternalLoad();

            Visible = ShowAssembled = false;
            SetProcess(false);
        }

        private void InternalLoad()
        {
            _collectSounds = new AudioStreamSample[8];
            for (int i = 0; i < _collectSounds.Length; i++)
            {
                string path = $"res://assets/Sounds/Collects/SplitUpCube/{_shardNotes[i]}.wav";
                _collectSounds[i] = ResourceLoader.Load<AudioStreamSample>(path);
            }

            for (int i = 0; i < CollectedCubes.Length; i++)
                CollectedCubes[i].Visible = (i + 1 <= GameState.SaveData.SmallCubes);

            Timer.Connect("timeout", this, nameof(TryAssembleCube));
        }

        public override void _Process(float delta)
        {
            Pivot.Transform = Pivot.Transform.Rotated(Vector3.Up, delta / 2f);
        }

        public void CollectCube(Transform cubeTransform)
        {
            Visible = true;
            SetProcess(true);
            int idx = GameState.SaveData.SmallCubes;

            GameState.SaveData.SmallCubes += 1;
            GameState.EmitSignal(nameof(GameStateManager.OnHudChanged));

            for (int i = 0; i < 8; i++)
                CollectedCubes[i].Visible = (i <= idx);
            
            Audio.Stream = _collectSounds[idx];
            Audio.Play();
            
            Timer.Stop();
            Timer.WaitTime = 3f;
            Timer.Start();

            Gomez gomez = GetParent<Gomez>();
            gomez.Signals.EmitSignal(nameof(ActionSignals.CollectedSmallCube));
        }

        private async void TryAssembleCube()
        {
            if (GameState.SaveData.SmallCubes >= 8)
            {
                ShowAssembled = true;
                SetProcess(false);
                GameState.SaveData.SmallCubes %= 8;

                Transform transform = GlobalTransform;
                Gomez gomez = GetParent<Gomez>();

                Pivot.Rotation = Vector3.Zero;
                gomez.CollectedTreasure = this;
                await ToSignal(gomez.Signals, nameof(ActionSignals.CollectedBigCube));

                GlobalTransform = transform;
                Visible = ShowAssembled = false;
            }
            else
            {
                GameState.SaveGame();
                Visible = false;
                SetProcess(false);
            }
        }
    }
}