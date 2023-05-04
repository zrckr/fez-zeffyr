using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using Zeffyr.Components;
using Zeffyr.Structure;
using Zeffyr.Tools;

namespace Zeffyr.Managers
{
    public class GameStateManager : Node
    {
        [Signal] public delegate void OnHudChanged();

        [Signal] public delegate void OnSaved();

        [Signal] public delegate void OnSceneChanged();

        [RootNode] protected TimeManager TimeManager;

        [RootNode] protected MusicManager MusicManager;

        [RootNode] protected FadeManager FadeManager;

        public string FallbackLevelPath { get; set; }

        public bool PauseOnLostFocus { get; set; }

        public bool Map { get; set; }
        
        public bool InGame { get; set; }

        public int SaveSlot { get; set; }
        
        public (int Volume, ActionType Action, Pickup Pickup)? NextChangeInfo { get; set; } 

        public SaveData SaveData
        {
            get
            {
                SaveData result = _saveDataSlots.ElementAtOrDefault(SaveSlot);
                if (result is null)
                    throw new NullReferenceException($"No SaveData found on slot {SaveSlot}");
                return result;
            }
        }

        private SceneTree Tree => GetTree();

        public string SaveFilePath => $"user://SaveSlot{SaveSlot}.json";
        
        public const string LevelPath = "res://assets/Levels/{0}.tscn";
        
        private SaveData _tempSaveData = new SaveData();
        private readonly SaveData[] _saveDataSlots = new SaveData[4];

        private Timer _saveTimer = new Timer()
        {
            Name = nameof(Timer),
            OneShot = true,
            WaitTime = 4f,
        };

        public GameStateManager()
        {
            PauseMode = PauseModeEnum.Process;
            SaveSlot = 0;

            for (int i = 0; i < _saveDataSlots.Length; i++)
                _saveDataSlots[i] = new SaveData();
        }

        public override void _Ready()
        {
            this.InjectNodes();
            this.AddChild(_saveTimer);
            _saveTimer.Connect("timeout", this, nameof(WriteSaveData));
        }

        public void LoadGame()
        {
            if (FileOperations.TryLoadJson(SaveFilePath, out _tempSaveData))
            {
                _saveDataSlots[SaveSlot] = _tempSaveData.Clone() as SaveData;
            }
            else
            {
                _saveDataSlots[SaveSlot] = new SaveData()
                {
                    IsNew = true
                };
            }
        }
        
        public void StartLoading()
        {
            // Stupid workaround for fixing double level loading
            if (Tree.CurrentScene is Level) return;

            string levelPath = FindLevel(SaveData.Level) ?? FallbackLevelPath;
            PackedScene scene = ResourceLoader.Load<PackedScene>(levelPath);
            Level level = scene.InstanceOrNull<Level>();
            
            if (level is not null)
            {
                Tree.Root.AddChild(level);
                Tree.CurrentScene = level;
                TimeManager.Reset();
                TimeManager.CurrentTime = DateTime.Today.AddTicks(SaveData.TimeOfDay);
            }
            else
                throw new NullReferenceException($"The scene does not inherit Level.cs script!");
        }

        private string FindLevel(string next, string previous = "")
        {
            if (!string.IsNullOrEmpty(next) && !next.Contains("tscn") && !next.Contains("res://"))
            {
                string lower = next.ToLower();
                string folder = previous.ToLower();
                
                string path1 = string.Format(LevelPath, lower);
                string path2 = string.Format(LevelPath, $"{folder}/{lower}");

                if (FileOperations.Exists(path2))
                    return path2;
                if (FileOperations.Exists(path1))
                    return path1;
                throw new InvalidOperationException($"{next} does not exists!");
            }
            return null;
        }
        
        public void ChangeLevel(string levelName, int volume = -1)
        {
            Level currentLevel = (Level) Tree.CurrentScene;
            string path = FindLevel(levelName, currentLevel.Name);
            
            PackedScene scene = ResourceLoader.Load<PackedScene>(path);
            Level nextLevel = scene.InstanceOrNull<Level>();
            
            if (nextLevel is null)
                throw new NullReferenceException($"{path} does not inherit Level.cs script!");
            
            Pickup pickup = currentLevel.Gomez.CarriedBody?.Duplicate() as Pickup;
            NextChangeInfo = (volume, currentLevel.Gomez.NextAction, pickup);
            
            if (currentLevel.Filename != levelName)
            {
                Tree.Root.RemoveChild(currentLevel);
                Tree.Root.AddChild(nextLevel);
                Tree.CurrentScene = nextLevel;
                currentLevel.QueueFree();
            }
            else
            {
                SaveData.View = currentLevel.GameCamera.Orthogonal;
                SaveData.TimeOfDay = TimeManager.CurrentTime.TimeOfDay.Ticks;
                MusicManager.Stop();
                TimeManager.Reset();
                Tree.ReloadCurrentScene();
            }
        }

        public void SaveGame()
        {
            _saveTimer.Start();
            _tempSaveData = (SaveData) SaveData.Clone();
            EmitSignal(nameof(OnSaved));
        }

        public void WriteSaveData()
        {
            _saveTimer.Stop();
            if (SaveData.SavedTime.HasValue)
            {
                _tempSaveData.IsNew = false;
                SaveData.IsNew = _tempSaveData.IsNew;
            }
            SaveData.SavedTime = DateTime.Now;
            Task.Run(() => FileOperations.TryWriteJson(_tempSaveData, SaveFilePath));
        }

        public void ClearSave()
        {
            if (SaveData != null)
            {
                SaveData.Clear();
                SaveData.IsNew = true;
                FileOperations.Remove(SaveFilePath);
            }
        }

        public void MakeNodeInactive(Node node, bool queueFree = false)
        {
            if (IsInstanceValid(node) || node != null)
            {
                string path = node.GetPath();
                if (!SaveData.ThisLevel.InactiveNodes.Contains(path))
                {
                    SaveData.ThisLevel.InactiveNodes.Add(path);
                }
                if (queueFree) node.QueueFree();
            }
        }
    }
}