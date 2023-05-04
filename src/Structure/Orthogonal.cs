using Godot;
using System;
using Zeffyr.Tools;

namespace Zeffyr.Structure
{
    public enum Orthogonal
    {
        None = 0,
        Front,
        Right,
        Back,
        Left,
        Up,
        Down,
    }

    public static class OrthogonalExtensions
    {
        public static int DistanceTo(this Orthogonal from, Orthogonal to)
        {
            int num = to - from;
            if (Mathf.Abs(num) == 3)
                num = -Mathf.Sign(num);
            return num;
        }

        public static Orthogonal GetOrthogonal(this Basis basis)
        {
            // We assume for the future that the XY plane will rotate, so we use the Z axis as the pivot point
            Vector3 forward = Mathz.OrthogonalBases[basis.GetOrthogonalIndex()].z;
            float dotF = forward.Dot(Vector3.Back);
            float dotR = forward.Dot(Vector3.Right);
            float dotU = forward.Dot(Vector3.Up);

            Orthogonal result = Orthogonal.None;
            result = dotF switch
            {
                +1f => Orthogonal.Front,
                -1f => Orthogonal.Back,
                _ => result,
            };
            result = dotR switch
            {
                +1f => Orthogonal.Right,
                -1f => Orthogonal.Left,
                _ => result,
            };
            result = dotU switch
            {
                +1f => Orthogonal.Up,
                -1f => Orthogonal.Down,
                _ => result,
            };
            return result;
        }
        
        public static Orthogonal Rotated(this Orthogonal fromView, int distance)
        {
            int num = (int)(fromView + distance);
            num = Mathf.Wrap(num, (int) Orthogonal.Front, (int) Orthogonal.Up);
            return (Orthogonal)num;
        }
        
        public static Basis GetBasis(this Orthogonal orthogonal)
        {
            int id = orthogonal switch
            {
                Orthogonal.Front => 0,
                Orthogonal.Right => 16,
                Orthogonal.Back => 10,
                Orthogonal.Left => 22,
                Orthogonal.Up => 12,
                Orthogonal.Down => 4,
                _ => throw new InvalidOperationException("Orthographic views only")
            };
            return Mathz.OrthogonalBases[id];
        }
    }
}