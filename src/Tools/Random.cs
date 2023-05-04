using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Zeffyr.Tools
{
    public static class Random
    {
        private readonly static RandomNumberGenerator Generator;

        static Random()
        {
            Generator = new RandomNumberGenerator();
            Generator.Seed = (ulong) System.Environment.TickCount;
            Generator.Randomize();
        }

        public static ulong Seed
        {
            get => Generator.Seed;
            set
            {
                Generator.Seed = value;
                Generator.Randomize();
            }
        }

        public static int Next(int value) => Generator.RandiRange(0, value);

        public static bool Probability(float value) => value > Generator.Randf();

        public static float Centered(float value) => (Generator.Randf() - 0.5f) * value * 2.0f;

        public static float Range(float min, float max) => Generator.RandfRange(min, max);

        public static int Range(int min, int max) => Generator.RandiRange(min, max);

        public static float Unit() => Generator.Randf();

        public static float Gaussian(float mean = 0f, float deviation = 0f)
            => Generator.Randfn(mean, deviation);

        public static IEnumerable<float> Sequence(int count, float min, float max)
            => Enumerable.Repeat(0, count).Select(_ => Range(min, max));

        public static T Choose<T>(this T[] array)
        {
            int idx = Next(array.Length - 1);
            return array[idx];
        }

        public static T Choose<T>(this List<T> list)
        {
            if (list is null || list.Count <= 0) return default(T);
            int idx = Next(list.Count - 1);
            return list[idx];
        }
    }
}