using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DebugVisuals
{
    [DefaultExecutionOrder(-1000)]
    public class DebugDrawer : MonoBehaviour
    {
        private static Material _lineMaterial;
        private static DebugDrawer _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (_instance == null)
            {
                GameObject debugDrawerObject = new GameObject("DebugDrawer");
                _instance = debugDrawerObject.AddComponent<DebugDrawer>();
                DontDestroyOnLoad(debugDrawerObject);
            }
        }

#if UNITY_EDITOR
        // Auto-hook into SceneView for Edit Mode drawing
        [InitializeOnLoadMethod]
        private static void EditorInit()
        {
            //SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            /*if (Event.current.type != EventType.Repaint) return;

            EnsureMaterial();
            _lineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            DebugDrawGL.Render();

            GL.PopMatrix();*/
        }
#endif

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            EnsureMaterial();
        }

        private void OnRenderObject()
        {
            if (_lineMaterial == null)
            {
                Debug.Log("No line Material");
                return;
            }

            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            DebugDrawGL.Render();

            GL.PopMatrix();
        }

        private static void EnsureMaterial()
        {
            if (_lineMaterial != null) return;

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            _lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 0);
        }
    }

    public static class DebugDrawGL
    {
        private struct Line
        {
            public Vector3 start;
            public Vector3 end;
            public Color color;
            public float endTime;
        }

        private const float EDITOR_LINE_DELAY = 0.001f;
        private static Vector3 xyRotationEuler = Vector3.zero;
        private static Vector3 xzRotationEuler = new Vector3(90, 0, 0);
        private static Vector3 yzRotationEuler = new Vector3(0, 90, 0);
        private static Quaternion xyRotation = Quaternion.Euler(xyRotationEuler);
        private static Quaternion yzRotation = Quaternion.Euler(yzRotationEuler);
        private static Quaternion xzRotation = Quaternion.Euler(xzRotationEuler);
        private static readonly List<Line> _lines = new List<Line>(500);

        // ───────────────────────────────────────
        // Public API
        // ───────────────────────────────────────
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            bool isInEditMode = !Application.isPlaying;
#else
        bool isInEditMode = false;
#endif

            float currentTime = Time.time;
            float durationEndTime = currentTime + duration;
            float defaultEndTime = isInEditMode ? currentTime + EDITOR_LINE_DELAY : currentTime;
            _lines.Add(new Line
            {
                start = start,
                end = end,
                color = color,
                endTime = duration > 0f ? durationEndTime : defaultEndTime
            });
        }

        public static void DrawDashedLine(Vector3 start, Vector3 end, Color color, float dashLength = 0.2f, float gapLength = 0.1f, float duration = 0f)
        {
            Vector3 direction = (end - start).normalized;
            float totalLength = Vector3.Distance(start, end);

            float segmentLength = dashLength + gapLength;
            int dashCount = Mathf.FloorToInt(totalLength / segmentLength);

            Vector3 pos = start;
            for (int i = 0; i < dashCount; i++)
            {
                Vector3 dashEnd = pos + direction * dashLength;
                DrawLine(pos, dashEnd, color, duration);
                pos += direction * segmentLength;
            }

            // Handle leftover part
            if (Vector3.Distance(pos, end) > 0.01f)
            {
                DrawLine(pos, end, color, duration);
            }
        }

        public static void DrawCircle(Vector3 center, float radius, Color color, Vector3 rotationEuler = default, int segments = 32, float duration = 0f)
        {
            Quaternion rotation = Quaternion.Euler(rotationEuler);

            float step = Mathf.PI * 2f / segments;
            Vector3 prev = rotation * new Vector3(Mathf.Cos(0), Mathf.Sin(0), 0) * radius + center;

            for (int i = 1; i <= segments; i++)
            {
                float angle = step * i;
                Vector3 next = rotation * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius + center;
                DrawLine(prev, next, color, duration);
                prev = next;
            }
        }

        public static void DrawWireSphere(Vector3 center, float radius, Color color, int segments = 32, float duration = 0f)
        {
            DrawCircle(center, radius, color, xyRotationEuler, segments, duration);
            DrawCircle(center, radius, color, xzRotationEuler, segments, duration);
            DrawCircle(center, radius, color, yzRotationEuler, segments, duration);
        }

        public static void DrawStadium(Vector3 center, float radius, float height, Color color, Vector3 rotationEuler = default, int segments = 32, float duration = 0f)
        {
            //Anything lower than 2 * radius sets height to 2 * radius
            Quaternion rotation = Quaternion.Euler(rotationEuler);
            int halfSegments = Mathf.FloorToInt(segments / 2f);
            float step = Mathf.PI * 2f / segments;
            float circleOffset = (height / 2f) - radius;
            Vector3 topPosition = center + rotation * Vector3.up * circleOffset;
            Vector3 bottomPosition = center + rotation * Vector3.down * circleOffset;
            float angle = 0;
            Vector3 prev = rotation * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius + topPosition;

            //Top
            for (int i = 1; i <= halfSegments; i++)
            {
                angle = step * i;
                Vector3 next = rotation * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius + topPosition;
                DrawLine(prev, next, color, duration);
                prev = next;
            }


            //Bottom
            angle = step * halfSegments;
            prev = rotation * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius + bottomPosition;
            for (int i = halfSegments + 1; i <= segments; i++)
            {
                angle = step * i;
                Vector3 next = rotation * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius + bottomPosition;
                DrawLine(prev, next, color, duration);
                prev = next;
            }

            //Side Lines
            Vector3 leftLineTopPos = rotation * new Vector3(Mathf.Cos(0), Mathf.Sin(0), 0) * radius + topPosition;
            Vector3 leftLineBottomPos = leftLineTopPos + rotation * Vector3.down * 2 * circleOffset;
            Vector3 rightLineTopPos = rotation * new Vector3(Mathf.Cos(step * halfSegments), Mathf.Sin(step * halfSegments), 0) * radius + bottomPosition;
            Vector3 rightLineBottomPos = rightLineTopPos + rotation * Vector3.up * 2 * circleOffset;
            DrawLine(leftLineTopPos, leftLineBottomPos, color, duration);
            DrawLine(rightLineTopPos, rightLineBottomPos, color, duration);
        }

        public static void DrawWireCapsule(Vector3 center, float radius, float height, Color color, Vector3 rotationEuler = default, int segments = 32, float duration = 0)
        {
            Quaternion rotation = Quaternion.Euler(rotationEuler);
            //Anything lower than 2 * radius sets height to 2 * radius
            float circleOffset = (height / 2f) - radius;
            Vector3 topPosition = center + rotation * Vector3.up * circleOffset;
            Vector3 bottomPosition = center + rotation * Vector3.down * circleOffset;

            DrawStadium(center, radius, height, color, (rotation * xyRotation).eulerAngles, segments, duration);
            DrawStadium(center, radius, height, color, (rotation * yzRotation).eulerAngles, segments, duration);
            DrawCircle(topPosition, radius, color, (rotation * xzRotation).eulerAngles, segments, duration);
            DrawCircle(bottomPosition, radius, color, (rotation * xzRotation).eulerAngles, segments, duration);
        }

        public static void DrawWireBox(Vector3 center, Vector3 size, Color color, float duration = 0f)
        {
            Vector3 half = size * 0.5f;

            Vector3[] c =
            {
            center + new Vector3(-half.x, -half.y, -half.z),
            center + new Vector3( half.x, -half.y, -half.z),
            center + new Vector3( half.x, -half.y,  half.z),
            center + new Vector3(-half.x, -half.y,  half.z),
            center + new Vector3(-half.x,  half.y, -half.z),
            center + new Vector3( half.x,  half.y, -half.z),
            center + new Vector3( half.x,  half.y,  half.z),
            center + new Vector3(-half.x,  half.y,  half.z),
        };

            // Bottom
            DrawLine(c[0], c[1], color, duration);
            DrawLine(c[1], c[2], color, duration);
            DrawLine(c[2], c[3], color, duration);
            DrawLine(c[3], c[0], color, duration);

            // Top
            DrawLine(c[4], c[5], color, duration);
            DrawLine(c[5], c[6], color, duration);
            DrawLine(c[6], c[7], color, duration);
            DrawLine(c[7], c[4], color, duration);

            // Vertical edges
            DrawLine(c[0], c[4], color, duration);
            DrawLine(c[1], c[5], color, duration);
            DrawLine(c[2], c[6], color, duration);
            DrawLine(c[3], c[7], color, duration);
        }

        public static void DrawArrow(Vector3 start, Vector3 end, Color color, float headLength = 0.25f, float headAngle = 20f, float duration = 0f)
        {
            DrawLine(start, end, color, duration);

            Vector3 dir = (end - start).normalized;
            Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 + headAngle, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 - headAngle, 0) * Vector3.forward;

            DrawLine(end, end + right * headLength, color, duration);
            DrawLine(end, end + left * headLength, color, duration);
        }

        public static void DrawGrid(Vector3 origin, int width, int height, float cellSize, Color color, float duration = 0f)
        {
            for (int x = 0; x <= width; x++)
            {
                Vector3 s = origin + new Vector3(x * cellSize, 0, 0);
                Vector3 e = s + new Vector3(0, 0, height * cellSize);
                DrawLine(s, e, color, duration);
            }
            for (int z = 0; z <= height; z++)
            {
                Vector3 s = origin + new Vector3(0, 0, z * cellSize);
                Vector3 e = s + new Vector3(width * cellSize, 0, 0);
                DrawLine(s, e, color, duration);
            }
        }


        // ───────────────────────────────────────
        // Render
        // ───────────────────────────────────────
        public static void Render()
        {
            if (_lines.Count == 0) return;

            GL.Begin(GL.LINES);

            float now = Time.time;
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                Line line = _lines[i];

                if (line.endTime >= now)
                {
                    GL.Color(line.color);
                    GL.Vertex(line.start);
                    GL.Vertex(line.end);

                    if (line.endTime < now + 0.001f)
                    {
                        RemoveLine(i);
                    }
                }
                else
                {
                    RemoveLine(i);
                }
            }

            GL.End();
        }

        private static void RemoveLine(int index)
        {
            _lines[index] = _lines[_lines.Count - 1];
            _lines.RemoveAt(_lines.Count - 1);
        }
    }
}

