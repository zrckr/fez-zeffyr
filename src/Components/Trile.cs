using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;

namespace Zeffyr.Components
{
    public class Trile : MeshInstance
    {
        [Export(PropertyHint.Layers3dPhysics)]
        public readonly int BackFace;
        
        [Export(PropertyHint.Layers3dPhysics)]
        public readonly int FrontFace;
        
        [Export(PropertyHint.Layers3dPhysics)]
        public readonly int LeftFace;
        
        [Export(PropertyHint.Layers3dPhysics)]
        public readonly int RightFace;

        [Export(PropertyHint.Layers3dPhysics)]
        protected readonly int CollisionMask;

        [Export(PropertyHint.Enum)]
        public readonly SurfaceType SurfaceType;

        [Export]
        public readonly Vector3 Size;

        [Export]
        public readonly bool CollisionOnly;

        public PhysicsLayer Mask => (PhysicsLayer) CollisionMask;

        public PhysicsLayer Layer => (PhysicsLayer) (BackFace | FrontFace | LeftFace | RightFace);
    }
}