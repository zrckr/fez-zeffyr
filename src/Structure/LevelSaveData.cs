using System;
using System.Collections.Generic;

namespace Zeffyr.Structure
{
    public class LevelSaveData : ICloneable
    {
        public HashSet<string> InactiveNodes { get; set; }

        public float? LastStableWaterHeight { get; set; }

        public WinConditions FilledConditions { get; set; }

        public bool FirstVisit { get; set; }

        public LevelSaveData()
        {
            InactiveNodes = new HashSet<string>();
            LastStableWaterHeight = null;
            FilledConditions = new WinConditions();
            FirstVisit = false;
        }

        public object Clone()
        {
            var levelSaveData = (LevelSaveData) MemberwiseClone();
            levelSaveData.FilledConditions = (WinConditions) FilledConditions.Clone();
            return levelSaveData;
        }
    }
}