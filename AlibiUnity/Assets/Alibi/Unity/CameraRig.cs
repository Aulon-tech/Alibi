using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Alibi.Unity
{
    public enum CameraMode { Overview, FirstPerson }

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

        public CameraMode Mode = CameraMode.Overview;

        // First-person settings
        public float MoveSpeed = 8f;
        public float LookSensitivity = 2f;
        public float EyeHeight = 1.65f;
        public float CollisionRadius = 0.3f;

        float currentYaw;
        float currentSize = 8f;
        Vector3 currentPos;
        bool initialized;
        readonly Vector3[] corners = new Vector3[4];

        // First-person state
        Vector3 fpPosition;
        float fpYaw;
        float fpPitch;
        bool fpLooking;
        float transitionT;
        Vector3 transitionFrom;
        Quaternion transitionFromRot;
        float transitionFromSize;
        bool transitioning;
        const float TransitionDuration = 0.6f;

        public static CameraRig Create(Transform parent, Color background)
        {
            var go = new GameObject("CameraRig");
            go.transform.SetParent(parent, false);
            var rig = go.AddComponent<CameraRig>();
            var camGo = new GameObject("AlibiCamera");
            camGo.transform.SetParent(go.transform, false);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 65f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 300f;
            cam.depth = 10;
            camGo.AddComponent<PhysicsRaycaster>();
            rig.Cam = cam;
            return rig;
        }

        public void SetMode(CameraMode mode)
        {
            if (mode == Mode) return;
            if (mode == CameraMode.FirstPerson)
            {
                fpPosition = Target.center + new Vector3(0, EyeHeight, 0);
                fpYaw = currentYaw;
                fpPitch = 0f;
            }
            transitioning = true;
            transitionT = 0f;
            transitionFrom = Cam.transform.position;
            transitionFromRot = Cam.transform.rotation;
            transitionFromSize = Cam.fieldOfView;
            Mode = mode;
        }

        public void EnterFirstPerson(Vector3 worldPos)
        {
            fpPosition = new Vector3(worldPos.x, worldPos.y + EyeHeight, worldPos.z);
            fpYaw = currentYaw;
            fpPitch = 0f;
            transitioning = true;
            transitionT = 0f;
            transitionFrom = Cam.transform.position;
            transitionFromRot = Cam.transform.rotation;
            transitionFromSize = Cam.fieldOfView;
            Mode = CameraMode.FirstPerson;
        }

        public void ExitFirstPerson()
        {
            transitioning = true;
            transitionT = 0f;
            transitionFrom = Cam.transform.position;
            transitionFromRot = Cam.transform.rotation;
            transitionFromSize = Cam.fieldOfView;
            Mode = CameraMode.Overview;
        }

        public void RotateStep(int dir)
        {
            if (Mode == CameraMode.FirstPerson)
                fpYaw += 45f * dir;
            else
                Yaw += 90f * dir;
        }

        public void ZoomBy(float factor)
        {
            if (Mode == CameraMode.FirstPerson)
            {
                Cam.fieldOfView = Mathf.Clamp(Cam.fieldOfView / factor, 40f, 90f);
            }
            else
            {
                Zoom = Mathf.Clamp(Zoom * factor, 0.7f, 2.6f);
            }
        }

        public void ResetView()
        {
            if (Mode == CameraMode.FirstPerson)
            {
                ExitFirstPerson();
            }
            Yaw = 45f;
            Zoom = 1f;
            Pan = Vector3.zero;
        }

        public void FocusOn(Vector3 world, float amount)
        {
            if (Mode == CameraMode.FirstPerson)
            {
                EnterFirstPerson(new Vector3(world.x, world.y, world.z));
            }
            else
            {
                Pan = Vector3.Lerp(Vector3.zero, world - Target.center, amount);
                Pan.y = 0;
            }
        }

        Rect ScreenFrame()
        {
            if (Frame == null) return new Rect(0, 0, Screen.width, Screen.height);
            Frame.GetWorldCorners(corners);
            float xMin = corners[0].x, yMin = corners[0].y, xMax = corners[2].x, yMax = corners[2].y;
            if (xMax - xMin < 10 || yMax - yMin < 10) return new Rect(0, 0, Screen.width, Screen.height);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        static bool PointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        void UpdateFirstPerson()
        {
            float dt = Time.unscaledDeltaTime;

            // Mouse look (right mouse button held)
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse != null && mouse.rightButton.isPressed && !PointerOverUI())
            {
                fpYaw += mouse.delta.x.ReadValue() * LookSensitivity * 0.15f;
                fpPitch -= mouse.delta.y.ReadValue() * LookSensitivity * 0.15f;
                fpPitch = Mathf.Clamp(fpPitch, -80f, 80f);
                fpLooking = true;
            }
            else
            {
                fpLooking = false;
            }

            // WASD movement
            Vector3 move = Vector3.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed) move.z += 1f;
                if (kb.sKey.isPressed) move.z -= 1f;
                if (kb.aKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed) move.x += 1f;
                if (kb.spaceKey.isPressed) move.y += 1f;
                if (kb.leftShiftKey.isPressed) move.y -= 1f;
            }
#else
            if (Input.GetMouseButton(1) && !PointerOverUI())
            {
                fpYaw += Input.GetAxis("Mouse X") * LookSensitivity * 2f;
                fpPitch -= Input.GetAxis("Mouse Y") * LookSensitivity * 2f;
                fpPitch = Mathf.Clamp(fpPitch, -80f, 80f);
                fpLooking = true;
            }
            else
            {
                fpLooking = false;
            }

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move.z += 1f;
            if (Input.GetKey(KeyCode.S)) move.z -= 1f;
            if (Input.GetKey(KeyCode.A)) move.x -= 1f;
            if (Input.GetKey(KeyCode.D)) move.x += 1f;
            if (Input.GetKey(KeyCode.Space)) move.y += 1f;
            if (Input.GetKey(KeyCode.LeftShift)) move.y -= 1f;
#endif

            if (move.sqrMagnitude > 0.01f)
            {
                move.Normalize();
                var yawRot = Quaternion.Euler(0f, fpYaw, 0f);
                var worldMove = yawRot * move;
                var desired = fpPosition + worldMove * MoveSpeed * dt;

                // Simple collision: raycast in move direction
                RaycastHit hit;
                if (Physics.SphereCast(fpPosition, CollisionRadius, worldMove, out hit, worldMove.magnitude * MoveSpeed * dt + 0.1f))
                {
                    if (hit.distance > CollisionRadius)
                        desired = fpPosition + worldMove * (hit.distance - CollisionRadius);
                    else
                        desired = fpPosition;
                }

                // Clamp Y so player stays near eye level within rooms
                desired.y = Mathf.Clamp(desired.y, -3f + EyeHeight, MapBuilder.UpperFloor + MapBuilder.WallHeight - 0.3f);
                fpPosition = desired;
            }
        }

        void LateUpdate()
        {
            if (Cam == null) return;

            if (transitioning)
            {
                transitionT += Time.unscaledDeltaTime / TransitionDuration;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(transitionT));

                Vector3 targetPos;
                Quaternion targetRot;
                float targetFov;

                if (Mode == CameraMode.FirstPerson)
                {
                    targetPos = fpPosition;
                    targetRot = Quaternion.Euler(fpPitch, fpYaw, 0f);
                    targetFov = 65f;
                }
                else
                {
                    ComputeOverview(out targetPos, out targetRot, out _);
                    targetFov = 65f;
                }

                Cam.transform.SetPositionAndRotation(
                    Vector3.Lerp(transitionFrom, targetPos, t),
                    Quaternion.Slerp(transitionFromRot, targetRot, t));
                Cam.fieldOfView = Mathf.Lerp(transitionFromSize, targetFov, t);

                if (transitionT >= 1f) transitioning = false;
                return;
            }

            if (Mode == CameraMode.FirstPerson)
            {
                UpdateFirstPerson();
                var rot = Quaternion.Euler(fpPitch, fpYaw, 0f);
                Cam.transform.SetPositionAndRotation(fpPosition, rot);
            }
            else
            {
                UpdateOverview();
            }
        }

        void ComputeOverview(out Vector3 pos, out Quaternion rot, out float size)
        {
            float dt = initialized ? Mathf.Clamp01(Time.unscaledDeltaTime * 6f) : 1f;
            float yaw = initialized ? Mathf.LerpAngle(currentYaw, Yaw, Orbit ? 1f : dt) : Yaw;
            rot = Quaternion.Euler(Pitch, yaw, 0f);
            var right = rot * Vector3.right;
            var up = rot * Vector3.up;
            var forward = rot * Vector3.forward;
            var center = Target.center + Pan;
            float halfW = 0.5f, halfH = 0.5f;
            var e = Target.extents;
            for (int i = 0; i < 8; i++)
            {
                var p = new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                halfW = Mathf.Max(halfW, Mathf.Abs(Vector3.Dot(p, right)));
                halfH = Mathf.Max(halfH, Mathf.Abs(Vector3.Dot(p, up)));
            }
            var frame = ScreenFrame();
            float sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
            float frameAspect = Mathf.Max(1, frame.width) / Mathf.Max(1, frame.height);
            float sceneAspect = halfW / halfH;
            float verticalHalf = sceneAspect > frameAspect ? halfW / frameAspect : halfH;
            size = verticalHalf * 1.12f / Zoom;

            // Perspective: position camera to fit the scene in view
            float fovRad = Cam.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float dist = size / Mathf.Tan(fovRad);
            dist = Mathf.Max(dist, 10f);

            float worldPerPixel = 2f * size / sh;
            float dx = (frame.center.x - sw * 0.5f) * worldPerPixel;
            float dy = (frame.center.y - sh * 0.5f) * worldPerPixel;
            pos = center - right * dx - up * dy - forward * dist;
        }

        void UpdateOverview()
        {
            if (Orbit) Yaw += OrbitSpeed * Time.deltaTime;
            float dt = initialized ? Mathf.Clamp01(Time.unscaledDeltaTime * 6f) : 1f;
            currentYaw = initialized ? Mathf.LerpAngle(currentYaw, Yaw, Orbit ? 1f : dt) : Yaw;

            Vector3 desired;
            Quaternion rot;
            float size;
            ComputeOverview(out desired, out rot, out size);

            currentSize = initialized ? Mathf.Lerp(currentSize, size, dt) : size;
            currentPos = initialized ? Vector3.Lerp(currentPos, desired, Orbit ? 1f : dt) : desired;
            Cam.transform.SetPositionAndRotation(currentPos, rot);
            initialized = true;
        }
    }
}
