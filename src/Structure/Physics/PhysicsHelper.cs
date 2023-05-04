using System;
using Godot;

namespace Zeffyr.Structure.Physics
{
    /******************************************************************************************************
    Based on: http://theinstructionlimit.com/behind-fez-collision-and-physics

    Fez is played from a 2D perspective. The collision results must match what the player sees.
    Knowing the collision type of each and every space (if filled), it’s easy to find the 1D “row” of possible
    colliders if you have the `2D` screen coordinates in hand. Then you just traverse front-to-back, and the
    first hit is kept, at which object you can early-out from the loop.
 
    Fez had 3 additional rules for the Z position or depth of the player:
    Rule 1:
        Gomez should stay visible. He should stay on-top of the world geometry as long as he does not
        rotate the viewpoint. This is done by correcting the depth such that Gomez stands right
        in front of the geometry.
 
    Rule 2:
        Gomez should never walk in mid-air. In 2D this is solved by the collision detection, but in the
        remaining axis it needs to be enforced, such that Gomez stands on the platform nearest to the camera.

    Rule 3:
        Otherwise, don’t change Gomez’s depth for no reason. The player expects it not to change.
 
    ********************************************************************************************************/

    public static class PhysicsHelper
    {
        public const float Snap = -0.25f;

        public const float FloorFriction = 0.05f;

        public const float WaterFriction = 0.075f;

        public const float SlideFriction = 0.2f;

        public const float AirFriction = 0.0025f;

        public static Vector3? TestAndCollide(
            IPhysicsEntity entity,
            bool wasOnFloor,
            float delta)
        {
            var space = entity.GetWorld().DirectSpaceState;
            PhysicsLayer mask = (PhysicsLayer) entity.CollisionMask;

            CollisionHelper.CameraTransform = entity.GetViewport().GetCamera().GlobalTransform;
            (CollisionInfo horizontal, CollisionInfo vertical) = CollisionHelper.IntersectRect(
                space, entity.GlobalTransform, entity.GlobalVelocity, entity.Size, mask, delta);

            bool inBackground = DetermineInBackground(entity);
            Vector3? clampToFloorDistance = new Vector3?();

            entity.Floor = null;
            entity.Ceiling = new CollisionInfo();
            entity.Wall = new CollisionInfo();

            if (vertical.Normal != Vector3.Zero)
            {
                if (entity.Velocity.y < 0f)
                {
                    entity.Floor = vertical.Collider as PhysicsBody;
                    clampToFloorDistance = vertical.Contact;
                }
                else if (entity.Velocity.y > 0f)
                    entity.Ceiling = vertical;
            }
            else
                TryAvoidNoCollision(entity, vertical, inBackground);

            if (horizontal.Normal != Vector3.Zero && Mathf.Abs(entity.Velocity.x) > 0.0f)
            {
                entity.Wall = horizontal;
            }
            else
                TryAvoidNoCollision(entity, horizontal, inBackground);

            float friction;
            if (entity.OnFloor | wasOnFloor)
                friction = (entity.IsSliding) ? SlideFriction : FloorFriction;
            else if (entity.IsSwimming)
                friction = WaterFriction;
            else
                friction = AirFriction;

            Vector3 velocity = entity.Velocity;
            velocity.x = Mathf.Lerp(velocity.x, 0f, friction);
            entity.Velocity = velocity;

            if (!entity.InBackground && inBackground)
                clampToFloorDistance = null;
            entity.InBackground = inBackground;

            return clampToFloorDistance;
        }

        public static Vector3 HugResponse(IPhysicsEntity entity)
        {
            Basis basis = entity.GlobalTransform.basis;
            Vector3 xy = basis.x + basis.y;
            Vector3 mask = Vector3.Zero;

            if (entity.Ceiling.Collider != null)
                mask = entity.Ceiling.Normal.Abs();

            PhysicsBody wall = entity.Wall.Collider as PhysicsBody;
            if (wall != null && entity.Wall.Normal != Vector3.Zero)
            {
                Vector3 wallSize = wall.ShapeOwnerGetExtents(entity.Wall.Shape);

                // Z-axis does not matter for aabb x aabb
                Vector3 position1 = wall.GlobalTransform.origin * xy;
                Vector3 position2 = entity.GlobalTransform.origin * xy;

                AABB aabb1 = new AABB(position1, wallSize);
                AABB aabb2 = new AABB(position2, entity.Size);
                Vector3 remain = aabb1.Intersection(aabb2).Size;

                // Entity must be outside wall's bounding othro-box
                if (remain == Vector3.Zero) mask = entity.Wall.Normal.Abs();
            }

            return entity.GlobalVelocity * (Vector3.One - mask);
        }

        public static bool TryAvoidNoCollision(
            IPhysicsEntity entity,
            CollisionInfo info,
            bool inBackground)
        {
            Basis basis = entity.GlobalTransform.basis;
            Vector3 z = basis.z;

            if (info.Collider != null && !entity.InBackground && info.Offset.HasValue)
            {
                Vector3 distance = info.Collider.GlobalTransform.origin + info.Offset.Value * (inBackground ? -1f : 1f);
                float z1 = entity.GlobalTransform.origin.Dot(z);
                float z2 = distance.Dot(z);

                if (z1 - z2 < 0f) // Entity is behind the object with 'no-collision'
                {
                    TryCorrectDepth(entity, distance);
                    return true;
                }
            }

            return false;
        }

        public static bool DetermineInBackground(IPhysicsEntity entity)
        {
            var space = entity.GetWorld().DirectSpaceState;
            Transform right = entity.GlobalTransform;

            CollisionObject obj = CollisionHelper.CastRect(space,
                right, entity.Size / 2f, PhysicsLayer.Solid | PhysicsLayer.Climbable, true).First;

            return obj is not null && obj.GetInstanceId() != entity.GetInstanceId();
        }

        public static Transform TryCorrectDepth(IPhysicsEntity entity, Vector3? distance)
        {
            if (distance.HasValue)
            {
                // Visible z-axis
                Vector3 forward = entity.Transform.basis.z.Abs();
                // Replace origin's z-axis with distance's z-axis 
                return new Transform()
                {
                    basis = entity.GlobalTransform.basis,
                    origin = distance.Value * forward + (Vector3.One - forward) * entity.GlobalTransform.origin,
                };
            }

            return entity.GlobalTransform;
        }

        public static PhysicsLayer GetLayer(this object obj)
        {
            uint collisionLayer = obj switch
            {
                Area area => area.CollisionLayer,
                PhysicsBody body => body.CollisionLayer,
                _ => 0
            };

            // Sometimes CollisionLayer field gives us the uint value with the sign bit enabled.
            // We need get rid of it to proper cast the value to enum.
            collisionLayer &= ~(1u << 31);
            return (PhysicsLayer) collisionLayer;
        }

        public static Vector3 ShapeOwnerGetExtents(this CollisionObject collisionObject, int shape)
        {
            Shape baseShape = collisionObject.ShapeOwnerGetShape(0, shape);
            return baseShape switch
            {
                BoxShape box => box.Extents,
                CapsuleShape capsule => new Vector3(capsule.Radius, capsule.Height, capsule.Radius),
                SphereShape sphere => Vector3.One * sphere.Radius,
                _ => throw new ArgumentException("Cannot get extents size from unknown shape!")
            };
        }

        public static void ShapeOwnerSetExtents(this CollisionObject collisionObject, int shape, Vector3 extents)
        {
            Shape baseShape = collisionObject.ShapeOwnerGetShape(0, shape);
            if (baseShape is BoxShape box)
            {
                box.Extents = extents;
            }

            if (baseShape is CapsuleShape capsule)
            {
                capsule.Radius = extents.x;
                capsule.Height = extents.y;
            }

            if (baseShape is SphereShape sphere)
            {
                sphere.Radius = extents.x;
            }
        }

        public static void ShapeOwnerUpdateDepth(this CollisionObject collisionObject, int shape)
        {
            Vector3 size = ShapeOwnerGetExtents(collisionObject, shape);
            size.z = CollisionHelper.MaxDepth;
            ShapeOwnerSetExtents(collisionObject, shape, size);
        }

        public static Vector3 ShapeOwnerGetXFormExtents(this CollisionObject collisionObject, int shape, Basis basis)
            => basis.XformInv(ShapeOwnerGetExtents(collisionObject, shape)).Abs();
    }
}