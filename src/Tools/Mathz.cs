using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zeffyr.Tools
{
    public static class Mathz
    {
        public const float TrixelsPerUnit = 16f;

        public const float Trixel = 1f / TrixelsPerUnit;

        public const float HalfTrixel = 1f / (TrixelsPerUnit * 2f);

        public static readonly Basis[] OrthogonalBases =
        {
            /*
             * This code was shamelessly stolen from the GodotSharp source code, because
             * for some reason such things are not available to ordinary mortals =/
             */
            GetBasis(1f, 0.0f, 0.0f, 0.0f, 1f, 0.0f, 0.0f, 0.0f, 1f),
            GetBasis(0.0f, -1f, 0.0f, 1f, 0.0f, 0.0f, 0.0f, 0.0f, 1f),
            GetBasis(-1f, 0.0f, 0.0f, 0.0f, -1f, 0.0f, 0.0f, 0.0f, 1f),
            GetBasis(0.0f, 1f, 0.0f, -1f, 0.0f, 0.0f, 0.0f, 0.0f, 1f),
            GetBasis(1f, 0.0f, 0.0f, 0.0f, 0.0f, -1f, 0.0f, 1f, 0.0f),
            GetBasis(0.0f, 0.0f, 1f, 1f, 0.0f, 0.0f, 0.0f, 1f, 0.0f),
            GetBasis(-1f, 0.0f, 0.0f, 0.0f, 0.0f, 1f, 0.0f, 1f, 0.0f),
            GetBasis(0.0f, 0.0f, -1f, -1f, 0.0f, 0.0f, 0.0f, 1f, 0.0f),
            GetBasis(1f, 0.0f, 0.0f, 0.0f, -1f, 0.0f, 0.0f, 0.0f, -1f),
            GetBasis(0.0f, 1f, 0.0f, 1f, 0.0f, 0.0f, 0.0f, 0.0f, -1f),
            GetBasis(-1f, 0.0f, 0.0f, 0.0f, 1f, 0.0f, 0.0f, 0.0f, -1f),
            GetBasis(0.0f, -1f, 0.0f, -1f, 0.0f, 0.0f, 0.0f, 0.0f, -1f),
            GetBasis(1f, 0.0f, 0.0f, 0.0f, 0.0f, 1f, 0.0f, -1f, 0.0f),
            GetBasis(0.0f, 0.0f, -1f, 1f, 0.0f, 0.0f, 0.0f, -1f, 0.0f),
            GetBasis(-1f, 0.0f, 0.0f, 0.0f, 0.0f, -1f, 0.0f, -1f, 0.0f),
            GetBasis(0.0f, 0.0f, 1f, -1f, 0.0f, 0.0f, 0.0f, -1f, 0.0f),
            GetBasis(0.0f, 0.0f, 1f, 0.0f, 1f, 0.0f, -1f, 0.0f, 0.0f),
            GetBasis(0.0f, -1f, 0.0f, 0.0f, 0.0f, 1f, -1f, 0.0f, 0.0f),
            GetBasis(0.0f, 0.0f, -1f, 0.0f, -1f, 0.0f, -1f, 0.0f, 0.0f),
            GetBasis(0.0f, 1f, 0.0f, 0.0f, 0.0f, -1f, -1f, 0.0f, 0.0f),
            GetBasis(0.0f, 0.0f, 1f, 0.0f, -1f, 0.0f, 1f, 0.0f, 0.0f),
            GetBasis(0.0f, 1f, 0.0f, 0.0f, 0.0f, 1f, 1f, 0.0f, 0.0f),
            GetBasis(0.0f, 0.0f, -1f, 0.0f, 1f, 0.0f, 1f, 0.0f, 0.0f),
            GetBasis(0.0f, -1f, 0.0f, 0.0f, 0.0f, -1f, 1f, 0.0f, 0.0f)
        };

        private static Basis GetBasis(float xx, float yx, float zx, float xy, float yy, float zy, float xz, float yz,
            float zz)
        {
            return new Basis()
            {
                Row0 = new Vector3(xx, yx, zx),
                Row1 = new Vector3(xy, yy, zy),
                Row2 = new Vector3(xz, yz, zz),
            };
        }

        public static Vector3 ToVector3(this Vector2 v) => new Vector3(v.x, v.y, 0f);

        public static Vector2 ToVector2(this Vector3 v) => new Vector2(v.x, v.y);

        public static float Clamp01(float value) => Mathf.Clamp(value, 0f, 1f);

        public static bool IsZeroApprox(this Vector3 v) =>
            Mathf.IsZeroApprox(v.x) && Mathf.IsZeroApprox(v.y) && Mathf.IsZeroApprox(v.z);

        public static bool IsZeroApprox(this Vector2 v) =>
            Mathf.IsZeroApprox(v.x) && Mathf.IsZeroApprox(v.y);

        public static float SmoothStep(float from, float to, float dt)
        {
            dt = Clamp01(dt);
            dt = (-2f * (dt * dt * dt) + 3f * (dt * dt));
            return (to * dt + from * (1.0f - dt));
        }

        public static bool In<T>(this T value, params T[] values)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values));
            return values.Contains(value);
        }

        public static IEnumerable<float> UnitRange(float step)
        {
            step = Clamp01(step);
            for (int i = 0; i < int.MaxValue; i++)
            {
                float value = step * i;
                if (value >= 1f) break;
                yield return value;
            }
        }
    }
}