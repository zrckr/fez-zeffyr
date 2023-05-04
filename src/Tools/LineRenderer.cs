using Godot;
using System.Collections.Generic;

namespace Zeffyr.Tools
{
    [Tool]
    public class LineRenderer : ImmediateGeometry
    {
        // The C# version of https://github.com/dbp8890/line-renderer
        
        public Vector3[] Points = new Vector3[] { Vector3.Zero, Vector3.One };
        public float StartThickness = 0.1f;
        public float EndThickness = 0.1f;
        public int CornerSmooth = 5;
        public int CapSmooth = 5;
        public bool DrawCaps = true;
        public bool DrawCorners = true;
        public bool GlobalCoords = true;
        public bool ScaleTexture = true;
        
        private Camera _camera;
        private Vector3 _cameraOrigin;

        public override void _Process(float delta)
        {
            if (Points.Length < 2)
                return;

            _camera = GetViewport().GetCamera();
            if (_camera == null)
                return;
            _cameraOrigin = ToLocal(_camera.GlobalTransform.origin);

            float progressStep = 1.0f / Points.Length;
            float progress = 0f;
            float thickness = Mathf.Lerp(StartThickness, EndThickness, progress);
            float nextThickness = Mathf.Lerp(StartThickness, EndThickness, progress + progressStep);

            Clear();
            Begin(Mesh.PrimitiveType.Triangles);

            for (int i = 0; i < Points.Length - 1; i++)
            {
                Vector3 a = Points[i];
                Vector3 b = Points[i + 1];

                if (GlobalCoords)
                {
                    a = ToLocal(a);
                    b = ToLocal(b);
                }

                Vector3 ab = b - a;
                Vector3 orthogonalAbStart = (_cameraOrigin - ((a + b) / 2f)).Cross(ab).Normalized() * thickness;
                Vector3 orthogonalAbEnd = (_cameraOrigin - ((a + b) / 2f)).Cross(ab).Normalized() * nextThickness;

                Vector3 aToAbStart = a + orthogonalAbStart;
                Vector3 aFromAbStart = a - orthogonalAbStart;
                Vector3 bToAbEnd = b + orthogonalAbEnd;
                Vector3 bFromAbEnd = b - orthogonalAbEnd;

                if (i == 0)
                    if (DrawCaps)
                        Cap(a, b, thickness, CapSmooth);

                if (ScaleTexture)
                {
                    float abLen = ab.Length();
                    float abFloor = Mathf.Floor(abLen);
                    float abFraction = abLen - abFloor;

                    SetUv(new Vector2(abFloor, 0));
                    AddVertex(aToAbStart);
                    SetUv(new Vector2(-abFraction, 0));
                    AddVertex(bToAbEnd);
                    SetUv(new Vector2(abFloor, 1));
                    AddVertex(aFromAbStart);
                    SetUv(new Vector2(-abFraction, 0));
                    AddVertex(bToAbEnd);
                    SetUv(new Vector2(-abFraction, 1));
                    AddVertex(bFromAbEnd);
                    SetUv(new Vector2(abFloor, 1));
                    AddVertex(aFromAbStart);
                }
                else
                {
                    SetUv(new Vector2(1, 0));
                    AddVertex(aToAbStart);
                    SetUv(new Vector2(0, 0));
                    AddVertex(bToAbEnd);
                    SetUv(new Vector2(1, 1));
                    AddVertex(aFromAbStart);
                    SetUv(new Vector2(0, 0));
                    AddVertex(bToAbEnd);
                    SetUv(new Vector2(0, 1));
                    AddVertex(bFromAbEnd);
                    SetUv(new Vector2(1, 1));
                    AddVertex(aFromAbStart);
                }

                if ((i == Points.Length - 2) && DrawCaps)
                {
                    Cap(b, a, nextThickness, CapSmooth);
                }
                else if (DrawCorners)
                {
                    Vector3 c = Points[i + 2];
                    if (GlobalCoords)
                        c = ToLocal(c);

                    Vector3 bc = c - b;
                    Vector3 orthogonalBcStart = (_cameraOrigin - ((b + c) / 2f)).Cross(bc).Normalized() * nextThickness;

                    float angleDot = ab.Dot(orthogonalBcStart);

                    if (angleDot > 0f)
                        Corner(b, bToAbEnd, b + orthogonalBcStart, CornerSmooth);
                    else
                        Corner(b, b - orthogonalBcStart, bFromAbEnd, CornerSmooth);
                }

                progress += progressStep;
                thickness = Mathf.Lerp(StartThickness, EndThickness, progress);
                nextThickness = Mathf.Lerp(StartThickness, EndThickness, progress + progressStep);
            }

            End();
        }

        private void Cap(Vector3 center, Vector3 pivot, float thickness, int smoothing)
        {
            Vector3 orthogonal = (_cameraOrigin - center).Cross(center - pivot).Normalized() * thickness;
            Vector3 axis = (center - _cameraOrigin).Normalized();

            var list = new List<Vector3>();
            for (int i = 0; i < smoothing + 1; i++)
                list.Add(Vector3.Zero);
            list[0] = center + orthogonal;
            list[smoothing] = center - orthogonal;

            for (int i = 1; i < smoothing; i++)
                list[i] = center + (orthogonal.Rotated(axis, Mathf.Lerp(0, Mathf.Pi, (float)i / smoothing)));

            for (int i = 1; i < smoothing + 1; i++)
            {
                SetUv(new Vector2(0, (float)(i - 1) / smoothing));
                AddVertex(list[i - 1]);
                SetUv(new Vector2(0, (float)(i - 1) / smoothing));
                AddVertex(list[i]);
                SetUv(new Vector2(0.5f, 0.5f));
                AddVertex(center);
            }
        }

        private void Corner(Vector3 center, Vector3 start, Vector3 end, int smoothing)
        {
            var list = new List<Vector3>();
            for (int i = 0; i < smoothing + 1; i++)
                list.Add(Vector3.Zero);
            list[0] = start;
            list[smoothing] = end;

            Vector3 axis = start.Cross(end).Normalized();
            Vector3 offset = start - center;
            float angle = offset.AngleTo(end - center);

            for (int i = 1; i < smoothing; i++)
                list[i] = center + offset.Rotated(axis, Mathf.Lerp(0, angle, (float)i / smoothing));

            for (int i = 1; i < smoothing + 1; i++)
            {
                SetUv(new Vector2(0, (float) (i - 1) / smoothing));
                AddVertex(list[i - 1]);
                SetUv(new Vector2(0, (float) (i - 1) / smoothing));
                AddVertex(list[i]);
                SetUv(new Vector2(0.5f, 0.5f));
                AddVertex(center);
            }
        }
    }
}