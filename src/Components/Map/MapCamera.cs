using Godot;
using Zeffyr.Managers;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;

namespace Zeffyr.Components.Map
{
    public class MapCamera : BaseCamera
    {
        [Export] public float[] ZoomCycle = new float[] { 80f, 40f, 20f, 10f, 5f };

        [Export(PropertyHint.Range, "0.1,1.0,0.1")]
        public float ZoomTime = 0.25f;

        [Export(PropertyHint.Range, "0.1,1.0,0.1")]
        public float RotateTime = 0.5f;

        [Export(PropertyHint.Range, "0.1,1.0,0.1")]
        public float TiltSpeed = 0.25f;

        [Export(PropertyHint.Range, "0.1,1.0,0.1")]
        public float PanSpeed = 0.125f;

        [Export] public Vector2 TiltAngles = new Vector2(45, 20);

        [Node] protected AudioStreamPlayer RotateLeft;
        [Node] protected AudioStreamPlayer RotateRight;
        [Node] protected AudioStreamPlayer ZoomIn;
        [Node] protected AudioStreamPlayer ZoomOut;
        [Node] protected AudioStreamPlayer Magnet;

        [RootNode] protected DebuggingBag DebuggingBag;

        public MapNode LastSelectedNode { get; private set; }
        public new int PixelsPerTrixel;    // Camera.Size is controlled by ZoomCycle
        private int _zoomIdx;
        private Vector3 _defaultRot;

        public override void _Ready()
        {
            base.PixelsPerTrixel = 1;
            base._Ready();
            this.InjectNodes();

            _defaultRot = Rotation;
            _zoomIdx = ZoomCycle.Length / 2;
            Size = ZoomCycle[_zoomIdx];
        }

        public override void _PhysicsProcess(float delta)
        {
            this._Tick(delta, () =>
            {
                Vector3 to = Vector3.Up * Mathf.Deg2Rad(90f);
                if (FzInput.IsJustPressed(InputAction.RotateLeft))
                {
                    RotateLeft.Play();
                    _defaultRot = Rotation - to;
                    return RotateAround(Rotation, Rotation - to, RotateTime);
                }

                if (FzInput.IsJustPressed(InputAction.RotateRight))
                {
                    RotateRight.Play();
                    _defaultRot = Rotation + to;
                    return RotateAround(Rotation, Rotation + to, RotateTime);
                }

                if (FzInput.IsJustPressed(InputAction.ZoomIn))
                {
                    int prev = _zoomIdx;
                    int next = Mathf.Clamp(_zoomIdx + 1, 0, ZoomCycle.Length - 1);
                    if (prev != next)
                    {
                        _zoomIdx += 1;
                        ZoomIn.Play();
                        return ZoomingSize(ZoomCycle[prev], ZoomCycle[next], ZoomTime);
                    }
                }

                if (FzInput.IsJustPressed(InputAction.ZoomOut))
                {
                    int prev = _zoomIdx;
                    int next = Mathf.Clamp(_zoomIdx - 1, 0, ZoomCycle.Length - 1);
                    if (prev != next)
                    {
                        _zoomIdx -= 1;
                        ZoomOut.Play();
                        return ZoomingSize(ZoomCycle[next], ZoomCycle[prev], ZoomTime, true);
                    }
                }

                SelectNode();
                TiltByInput(_defaultRot, FzInput.FreeLook, TiltAngles, TiltSpeed);
                PanByInput(FzInput.Movement, PanSpeed, ZoomCycle[_zoomIdx] / 2f);
                return null;
            });
        }

        private void SelectNode()
        {
            Vector2 select = GetViewport().GetMousePosition();

            PhysicsDirectSpaceState spaceState = GetWorld().DirectSpaceState;
            Vector3 from = ProjectRayOrigin(select);
            Vector3 to = from + ProjectRayNormal(select) * 1000f;

            CollisionInfo intersection = CollisionHelper.IntersectRaycast(spaceState, from, to, (uint) PhysicsLayer.MapNode, true);
            
            if (intersection.Collider != null)
            {
                if (LastSelectedNode != null)
                {
                    MapNode targetNode = intersection.Collider.GetParentOrNull<MapNode>();
                    if (LastSelectedNode != targetNode)
                    {
                        targetNode?.Pop(true);
                        LastSelectedNode = targetNode;
                        targetNode?.Pop(false);
                        Magnet.Play();
                    }
                }
                else
                {
                    LastSelectedNode = intersection.Collider.GetParentOrNull<MapNode>();
                    LastSelectedNode.Pop(false);
                }
            }
            else if (LastSelectedNode != null)
            {
                LastSelectedNode.Pop(true);
                LastSelectedNode = null;
            }
        }
    }
}