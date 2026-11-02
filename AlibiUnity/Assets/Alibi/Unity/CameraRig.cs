using UnityEngine;
using UnityEngine.EventSystems;

namespace Alibi.Unity
{
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Cam;
        public Bounds Target;
        public RectTransform Frame;
        public float Pitch = 34f;
        public float Yaw = 45f;
        public float Zoom = 1f;
        public bool Orbit;
        public float OrbitSpeed = 6f;
        public Vector3 Pan;

        float currentYaw;
        float currentSize = 8f;
        Vector3 currentPos;
        bool initialized;
        readonly Vector3[] corners = new Vector3[4];

        public static CameraRig Create(Transform parent, Color background)
        {
            var go = new GameObject("CameraRig");
            go.transform.SetParent(parent, false);
            var rig = go.AddComponent<CameraRig>();
            var camGo = new GameObject("AlibiCamera");
            camGo.transform.SetParent(go.transform, false);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.depth = 10;
            camGo.AddComponent<PhysicsRaycaster>();
            rig.Cam = cam;
            return rig;
        }

        public void RotateStep(int dir)
        {
            Yaw += 90f * dir;
        }

        public void ZoomBy(float factor)
        {
            Zoom = Mathf.Clamp(Zoom * factor, 0.7f, 2.6f);
        }

        public void ResetView()
        {
            Yaw = 45f;
            Zoom = 1f;
            Pan = Vector3.zero;
        }

        public void FocusOn(Vector3 world, float amount)
        {
            Pan = Vector3.Lerp(Vector3.zero, world - Target.center, amount);
            Pan.y = 0;
        }

        Rect ScreenFrame()
        {
            if (Frame == null) return new Rect(0, 0, Screen.width, Screen.height);
            Frame.GetWorldCorners(corners);
            float xMin = corners[0].x, yMin = corners[0].y, xMax = corners[2].x, yMax = corners[2].y;
            if (xMax - xMin < 10 || yMax - yMin < 10) return new Rect(0, 0, Screen.width, Screen.height);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        void LateUpdate()
        {
            if (Cam == null) return;
            if (Orbit) Yaw += OrbitSpeed * Time.deltaTime;
            float dt = initialized ? Mathf.Clamp01(Time.unscaledDeltaTime * 6f) : 1f;
            currentYaw = initialized ? Mathf.LerpAngle(currentYaw, Yaw, Orbit ? 1f : dt) : Yaw;
            var rot = Quaternion.Euler(Pitch, currentYaw, 0f);
            var right = rot * Vector3.right;
            var up = rot * Vector3.up;
            var forward = rot * Vector3.forward;
            var center = Target.center + Pan;
            float halfW = 0.5f, halfH = 0.5f;
            var e = Target.extents;
            for (int i = 0; i < 8; i++)
            {
                var p = Target.center + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z) - Target.center;
                halfW = Mathf.Max(halfW, Mathf.Abs(Vector3.Dot(p, right)));
                halfH = Mathf.Max(halfH, Mathf.Abs(Vector3.Dot(p, up)));
            }
            var frame = ScreenFrame();
            float sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
            float size = Mathf.Max(halfW * sh / Mathf.Max(1, frame.width), halfH * sh / Mathf.Max(1, frame.height)) * 1.12f / Zoom;
            currentSize = initialized ? Mathf.Lerp(currentSize, size, dt) : size;
            float worldPerPixel = 2f * currentSize / sh;
            float dx = (frame.center.x - sw * 0.5f) * worldPerPixel;
            float dy = (frame.center.y - sh * 0.5f) * worldPerPixel;
            var desired = center - right * dx - up * dy - forward * 60f;
            currentPos = initialized ? Vector3.Lerp(currentPos, desired, Orbit ? 1f : dt) : desired;
            Cam.orthographicSize = currentSize;
            Cam.transform.SetPositionAndRotation(currentPos, rot);
            initialized = true;
        }
    }
}
