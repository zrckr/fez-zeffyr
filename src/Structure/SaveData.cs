using System;
using System.Collections.Generic;

namespace Zeffyr.Structure
{
    public class SaveData : ICloneable
    {
        public bool IsNew { get; set; }
        
        public DateTime CreationTime { get; set; }
        
        public long PlayTime { get; set; }
        
        public DateTime? SavedTime { get; set; }

        public string Level { get; set; }
        
        public Orthogonal View { get; set; }
        
        public float[] Floor { get; set; }
        
        public long TimeOfDay { get; set; }

        public int Keys { get; set; }
        
        public int BigCubes { get; set; }
        
        public int AntiCubes { get; set; }
        
        public int SmallCubes { get; set; }
        
        public bool ScoreDirty { get; set; }
        
        public float? GlobalWaterHeight { get; set; }

        public Dictionary<string, LevelSaveData> World { get; set; }
        
        public LevelSaveData ThisLevel
        {
            get
            {
                if (!string.IsNullOrEmpty(Level))
                {
                    if (World.TryGetValue(this.Level, out LevelSaveData levelSaveData))
                    {
                        return levelSaveData;
                    }
                    else
                    {
                        World[Level] = new LevelSaveData();
                        return World[Level];
                    }
                }
                else
                    throw new NullReferenceException("Level name is not set!");
            }
        }

        public SaveData()
        {
            Clear();
            CreationTime = DateTime.Now;
        }

        public void Clear()
        {
            PlayTime = 0;
            Level = null;
            View = Orthogonal.None;
            Floor = new float[3];
            SavedTime = null;
            TimeOfDay = TimeSpan.FromHours(12.0).Ticks;
            AntiCubes = BigCubes = Keys = SmallCubes = 0;
            ScoreDirty = false;
            GlobalWaterHeight = new float?();
            World = new Dictionary<string, LevelSaveData>();
            IsNew = true;
        }

        public object Clone()
        {
            var saveData = (SaveData) MemberwiseClone();
            foreach (string key in World.Keys)
            {
                if (!saveData.World.ContainsKey(key))
                    saveData.World[key] = (LevelSaveData) World[key].Clone();
            }
            foreach (string key in saveData.World.Keys)
            {
                if (!World.ContainsKey(key))
                    saveData.World.Remove(key);
            }
            return saveData;
        }
    }
}