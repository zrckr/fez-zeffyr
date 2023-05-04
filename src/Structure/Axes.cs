using Godot;

namespace Zeffyr.Structure
{
    public readonly struct Axes
    {
        public Vector3 Facing { get; }

        public Vector3 Forward { get; }

        public Vector3 Right { get; }

        public Vector3 Up { get; }

        public Vector3 XMask { get; }
        
        public Vector3 YMask { get; }

        public Vector3 ZMask { get; }
        
        public Vector3 XyMask { get; }
        
        public Vector3 XzMask { get; }
        
        public Vector3 YzMask { get; }
        
        public Axes(Basis basis, float facing = 1f)
        {
            basis.Scale = Vector3.One;
            
            Right = basis.x.Normalized();
            Up = basis.y.Normalized();
            Forward = basis.z.Normalized();
            Facing = Right * facing;
            
            XMask = Right.Abs();
            YMask = Up.Abs();
            ZMask = Forward.Abs();

            XyMask = XMask + YMask;
            XzMask = XMask + ZMask;
            YzMask = YMask + ZMask;
        }

        public override string ToString() => $"r: {Right}, u: {Up}, f: {Forward}";
    }
}