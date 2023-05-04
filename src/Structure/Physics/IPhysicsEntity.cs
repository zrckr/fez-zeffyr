using Godot;

namespace Zeffyr.Structure.Physics
{
    public interface IPhysicsEntity
    {
        Transform Transform { get; set; }

        Transform GlobalTransform { get; set; }

        Vector3 Origin { get; set; }

        uint CollisionLayer { get; }

        uint CollisionMask { get; }

        bool InBackground { get; set; }

        World GetWorld();

        Viewport GetViewport();

        Vector3 Size { get; }

        Vector3 Velocity { get; set; }

        Vector3 GlobalVelocity { get; set; }

        PhysicsBody Floor { get; set; }

        CollisionInfo Wall { get; set; }

        CollisionInfo Ceiling { get; set; }

        bool IsSwimming { get; }

        bool IsSliding { get; }

        bool IsClimbing { get; }

        bool OnFloor { get; }

        bool OnWall { get; }

        bool OnCeiling { get; }

        ulong GetInstanceId();
    }
}