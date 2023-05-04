using Godot;

namespace Zeffyr.Structure.Physics
{
    public struct CollisionInfo
    {
        public Vector3? Contact;

        public Vector3? Offset;

        public Vector3 Normal;

        public Vector3? Velocity;

        public CollisionObject Collider;

        public int ColliderId;

        public int Shape;

        public override string ToString()
        {
            if (Collider == null) return $"{{ None }}";
            return $"{{ {Collider.Name} @ {Collider.GlobalTransform.origin} N {Normal} C {Contact} S {Shape} }}";
        }
    }
}