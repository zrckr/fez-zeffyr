using System;
using System.Linq;

namespace Zeffyr.Structure
{
    public class WinConditions : ICloneable
    {
        public int LockedDoors { get; set; }

        public int UnlockedDoors { get; set; }

        public int Chests { get; set; }

        public int BigCubes { get; set; }

        public int OtherCollectibles { get; set; }

        public int SmallCubes { get; set; }

        public int Secrets { get; set; }

        public bool Fills(WinConditions w)
        {
            bool[] flags = new bool[]
            {
                UnlockedDoors >= w.UnlockedDoors,
                LockedDoors >= w.LockedDoors,
                Chests >= w.Chests,
                BigCubes >= w.BigCubes,
                OtherCollectibles >= w.OtherCollectibles,
                SmallCubes >= w.SmallCubes,
                Secrets >= w.Secrets,
            };
            return !flags.Contains(false);
        }

        public object Clone() => MemberwiseClone();
    }
}