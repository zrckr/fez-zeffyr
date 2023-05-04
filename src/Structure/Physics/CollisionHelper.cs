using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using Zeffyr.Tools;
using Array = Godot.Collections.Array;
using Object = Godot.Object;

namespace Zeffyr.Structure.Physics
{
    public static class CollisionHelper
    {
        public const int MaxResults = 512;

        public const float Margin = 0.07f;

        public const float MaxDepth = 100f;

        public const float MinDepth = 0.3125f;
        
        public static Transform CameraTransform { get; set; }

        #region ----------------------------------------------- Extensions ---------------------------------------------

        public static (CollisionInfo, CollisionInfo) IntersectRect(
            PhysicsDirectSpaceState space,
            Transform transform,
            Vector3 velocity,
            Vector3 halfSize,
            PhysicsLayer mask,
            float delta,
            float depth = MaxDepth)
        {
            Vector3 vx = transform.basis.x.Abs() * velocity;
            Vector3 vy = transform.basis.y.Abs() * velocity;

            CollisionInfo horizontal = IntersectLine(space, transform, vx, halfSize, mask, Vector2.Axis.X,
                delta, depth);
            CollisionInfo vertical = IntersectLine(space, transform, vy, halfSize, mask, Vector2.Axis.Y,
                delta, depth);

            return (horizontal, vertical);
        }

        private static CollisionInfo IntersectLine(
            PhysicsDirectSpaceState space,
            Transform transform,
            Vector3 velocity,
            Vector3 halfSize,
            PhysicsLayer mask,
            Vector2.Axis axis,
            float delta,
            float depth = MaxDepth)
        {
            Vector3 plane = (axis == Vector2.Axis.X)
                ? new Vector3(0f, halfSize.y - Margin, depth)
                : new Vector3(halfSize.x - Margin, 0f, depth);

            float offset = ((axis == Vector2.Axis.X) ? halfSize.x : halfSize.y);

            Transform next = transform;
            next.origin += velocity * delta + offset * velocity.Sign();
            
            CollisionInfo[] results = space.IntersectBoxcastAll(next, plane, (uint) mask);

            // Sort the results relative to the global camera position.
            Transform farPoint = CameraTransform;
            farPoint.origin = CameraTransform.origin.Ceil();

            CollisionInfo result = results
                .OrderByDescending(c => c.Collider.GlobalTransform.origin.DistanceSquaredTo(farPoint.origin))
                .LastOrDefault();

            // Since CollideShape doesn't provide any useful collision info (normals, contacts etc.),
            // we gonna generate this info, based on the collision layer and the velocity direction.
            if (result.Collider != null)
            {
                // Fallback if velocity equals zero
                if (velocity.IsZeroApprox()) return result;

                PhysicsLayer layer = result.Collider.GetLayer();
                float y = velocity.Dot(transform.basis.y);

                // Z-axis does not matter for aabb x aabb
                Vector3 xy = (transform.basis.x + transform.basis.y).Abs();
                Vector3 position1 = result.Collider.GlobalTransform.origin * xy;
                Vector3 position2 = transform.origin * xy;

                Vector3 wallSize = result.Collider.ShapeOwnerGetExtents(result.Shape);
                AABB aabb1 = new AABB(position1, wallSize * 2f);
                AABB aabb2 = new AABB(position2, halfSize * 2f);

                // Entity must be outside wall's bounding othro-box
                if (aabb1.Intersects(aabb2) && !layer.HasFlag(PhysicsLayer.AllSides))
                    return new CollisionInfo();

                if (layer.HasFlag(PhysicsLayer.Background) || (layer.HasFlag(PhysicsLayer.TopOnly) && y >= 0f))
                {
                    Vector3 extents = result.Collider.ShapeOwnerGetExtents(result.Shape);
                    result.Offset = transform.basis.z * extents.z * 2f;
                }

                if (layer.HasFlag(PhysicsLayer.AllSides) || (layer.HasFlag(PhysicsLayer.TopOnly) && y < 0f))
                {
                    result.Contact = result.Collider.GlobalTransform.origin;
                    result.Normal = -velocity.Sign();
                }
            }

            return result;
        }

        public static (CollisionObject Near, CollisionObject Far, CollisionObject First) CastRect(
            this PhysicsDirectSpaceState space,
            Transform transform,
            Vector3 size,
            PhysicsLayer mask,
            bool backwards = false)
        {
            return CastRect<CollisionObject>(space, transform, size, mask, backwards);
        }
        
        public static (T Near, T Far, T First) CastRect<T>(
            this PhysicsDirectSpaceState space,
            Transform transform,
            Vector3 size,
            PhysicsLayer mask,
            bool backwards = false) where T: class
        {
            Transform farPoint = transform;
            farPoint.origin = (transform.origin + transform.basis.z * MaxDepth * 2f).Ceil();

            uint checkMask = (uint) (PhysicsLayer.Solid | mask);
            Vector3 resize = new Vector3(size.x - Margin, size.y - Margin, MaxDepth);

            CollisionInfo[] results = space.IntersectBoxcastAll(transform, resize, checkMask, true);

            CollisionObject[] sorted = results.Select(c => c.Collider)
                .OrderByDescending(c => c.GlobalTransform.origin.DistanceSquaredTo(farPoint.origin)).ToArray();

            CollisionObject[] behind = sorted.Where(c =>
                c.GlobalTransform.origin.Dot(transform.basis.z) > transform.origin.Dot(transform.basis.z)).ToArray();

            CollisionObject near, far;
            if (backwards)
            {
                near = behind.FirstOrDefault();
                far = behind.ElementAtOrDefault(1);
            }
            else
            {
                near = sorted.LastOrDefault();
                far = sorted.ElementAtOrDefault(sorted.Count() - 2);
            }

            int inc = 0;
            while (CheckXy(near, far, transform.basis))
            {
                far = (backwards)
                    ? behind.ElementAtOrDefault(2 + inc)
                    : sorted.ElementAtOrDefault(sorted.Count() - (3 + inc));
                inc += 1;
            }

            PhysicsLayer nearLayer = near.GetLayer(); 
            PhysicsLayer farLayer = far.GetLayer();
            if ((nearLayer & mask) == 0)
                near = null;
            if ((farLayer & mask) == 0)
                far = null;

            T nearT = near as T;
            T farT = far as T;
            T first = !Equals(near, default(T)) ? nearT : farT;
            return (nearT, farT, first);
        }

        public static T CastPoint<T>(
            this PhysicsDirectSpaceState space,
            Transform transform,
            PhysicsLayer mask,
            bool backwards = false) where T: class
        {
            return CastRect<T>(space, transform, Vector3.One * Mathz.HalfTrixel, mask, backwards).First;
        }
        
        public static CollisionObject CastPoint(
            this PhysicsDirectSpaceState space,
            Transform transform,
            PhysicsLayer mask,
            bool backwards = false)
        {
            return CastPoint<CollisionObject>(space, transform, mask, backwards);
        }

        private static bool CheckXy(Spatial first, Spatial second, Basis basis)
        {
            if (second != null)
            {
                Vector2 xy1 = basis.XformInv(first.GlobalTransform.origin).ToVector2();
                Vector2 xy2 = basis.XformInv(second.GlobalTransform.origin).ToVector2();
                return xy1.IsEqualApprox(xy2);
            }

            return false;
        }

        #endregion

        #region ----------------------------------------------- Wrappers -----------------------------------------------

        public static CollisionInfo GetCollisionInfo(
            this KinematicCollision kc)
        {
            if (kc == null) return new CollisionInfo();
            return new CollisionInfo
            {
                Contact = kc.Position,
                Normal = kc.Normal,
                Collider = kc.Collider as CollisionObject,
                ColliderId = (int) kc.ColliderId,
                Shape = kc.ColliderShapeIndex,
                Velocity = kc.ColliderVelocity,
            };
        }

        public static CollisionInfo GetCollisionInfo(
            this RayCast rc)
        {
            if (!rc.IsColliding()) return new CollisionInfo();
            return new CollisionInfo
            {
                Contact = rc.GetCollisionPoint(),
                Normal = rc.GetCollisionNormal(),
                Collider = rc.GetCollider() as CollisionObject,
                ColliderId = (int) rc.GetCollider().GetInstanceId(),
                Shape = rc.GetColliderShape(),
            };
        }

        public static CollisionInfo[] IntersectBoxcastAll(
            this PhysicsDirectSpaceState spaceState,
            Transform transform,
            Vector3 size,
            uint mask = int.MaxValue,
            bool includeAreas = false,
            bool includeBodies = true,
            float margin = Margin,
            int maxResults = MaxResults)
        {
            var query = new PhysicsShapeQueryParameters
            {
                Transform = transform,
                CollisionMask = (int) mask,
                Margin = margin,
                CollideWithAreas = includeAreas,
                CollideWithBodies = includeBodies
            };

            BoxShape shape = new BoxShape();
            shape.Extents = size;
            query.SetShape(shape);

            var dictionaries = spaceState.IntersectShape(query, maxResults).Cast<Dictionary>();

            List<CollisionInfo> infos = new List<CollisionInfo>();
            foreach (var dictionary in dictionaries)
            {
                var info = new CollisionInfo
                {
                    Collider = dictionary["collider"] as CollisionObject,
                    ColliderId = (int) dictionary["collider_id"],
                    Shape = (int) dictionary["shape"],
                };
                infos.Add(info);
            }

            return infos.ToArray();
        }

        public static CollisionInfo IntersectRaycast(
            this PhysicsDirectSpaceState spaceState,
            Vector3 from,
            Vector3 to,
            uint mask = int.MaxValue,
            bool includeAreas = false,
            bool includeBodies = true)
        {
            var dictionary = spaceState.IntersectRay(from, to, null, mask, includeBodies, includeAreas);
            if (dictionary.Count > 0)
            {
                var info = new CollisionInfo
                {
                    Collider = dictionary["collider"] as CollisionObject,
                    ColliderId = (int) dictionary["collider_id"],
                    Normal = (Vector3) dictionary["normal"],
                    Contact = (Vector3) dictionary["position"],
                    Shape = (int) dictionary["shape"],
                };
                return info;
            }

            return new CollisionInfo();
        }

        public static CollisionInfo IntersectBoxcast(
            this PhysicsDirectSpaceState spaceState,
            Transform transform,
            Vector3 size,
            Object[] exclude = null,
            uint mask = int.MaxValue,
            float margin = Margin,
            bool includeAreas = false,
            bool includeBodies = true)
        {
            IEnumerable<Object> array = exclude ?? new Object[0];
            var query = new PhysicsShapeQueryParameters
            {
                Transform = transform,
                Exclude = new Array(array),
                CollisionMask = (int) mask,
                Margin = margin,
                CollideWithAreas = includeAreas,
                CollideWithBodies = includeBodies
            };

            BoxShape shape = new BoxShape();
            shape.Extents = size;
            query.SetShape(shape);

            var dictionary = spaceState.GetRestInfo(query);
            if (dictionary.Count > 0)
            {
                var spatial = GD.InstanceFromId(Convert.ToUInt64(dictionary["collider_id"]));
                var info = new CollisionInfo
                {
                    Collider = spatial as CollisionObject,
                    ColliderId = (int) dictionary["collider_id"],
                    Normal = (Vector3) dictionary["normal"],
                    Contact = (Vector3) dictionary["point"],
                    Shape = (int) dictionary["shape"],
                };
                return info;
            }

            return new CollisionInfo();
        }

        #endregion
    }
}