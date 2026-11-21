using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Alibi.Unity
{
    public enum CameraMode { Overview, FirstPerson, ThirdPerson, TopDown }

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

        // Third-person settings
        public float TpDistance = 3.5f;
        public float TpHeight = 0.5f;
        public Transform PlayerBody;

        // Top-down (Pamje nga lart) settings
        public float TopDownDistance = 5.5f;
        public float TopDownPitch = 42f;
        public float TopDownMaxPitch = 80f;
        public float TopDownTargetHeight = 1.1f;
        float tdPitch = -1f;

        float currentYaw;
        float currentSize = 8f;
        Vector3 currentPos;
        bool initialized;
        readonly Vector3[] corners = new Vector3[4];

        // First-person state
        Vector3 fpPosition;
        float fpYaw;
        float fpPitch;

        float transitionT;
        Vector3 transitionFrom;
        Quaternion transitionFromRot;
        float transitionFromSize;
        bool transitioning;
        const float TransitionDuration = 0.6f;

        // Turn speed for A/D rotation (degrees per second)
        public float TurnSpeed = 120f;

        // Public accessors for GameManager proximity checks
        public Vector3 FpPosition { get { return fpPosition; } }
        public float FpYaw { get { return fpYaw; } }
        public bool IsCursorLocked { get { return false; } }

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
            if (mode == CameraMode.FirstPerson || mode == CameraMode.ThirdPerson || mode == CameraMode.TopDown)
            {
                if (Mode == CameraMode.Overview)
                {
                    fpPosition = Target.center + new Vector3(0, EyeHeight, 0);
                    fpYaw = currentYaw;
                    fpPitch = 0f;
                }
            }
            transitioning = true;
            transitionT = 0f;
            transitionFrom = Cam.transform.position;
            transitionFromRot = Cam.transform.rotation;
            transitionFromSize = Cam.fieldOfView;
            Mode = mode;
            UpdatePlayerBody();
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
            UpdatePlayerBody();
        }

        public void EnterThirdPerson(Vector3 worldPos)
        {
            fpPosition = new Vector3(worldPos.x, worldPos.y + EyeHeight, worldPos.z);
            fpYaw = currentYaw;
            fpPitch = 0f;
            transitioning = true;
            transitionT = 0f;
            transitionFrom = Cam.transform.position;
            transitionFromRot = Cam.transform.rotation;
            transitionFromSize = Cam.fieldOfView;
            Mode = CameraMode.ThirdPerson;
            UpdatePlayerBody();
        }

        public void ExitFirstPerson()
        {
            transitioning = true;
            transitionT = 0f;
            transitionFrom = Cam.transform.position;
            transitionFromRot = Cam.transform.rotation;
            transitionFromSize = Cam.fieldOfView;
            Mode = CameraMode.Overview;
            UpdatePlayerBody();
        }

        public void TogglePersonView()
        {
            if (Mode == CameraMode.FirstPerson)
                SetMode(CameraMode.ThirdPerson);
            else if (Mode == CameraMode.ThirdPerson)
                SetMode(CameraMode.FirstPerson);
        }

        public void RotateStep(int dir)
        {
            if (Mode == CameraMode.FirstPerson || Mode == CameraMode.ThirdPerson || Mode == CameraMode.TopDown)
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
            else if (Mode == CameraMode.ThirdPerson)
            {
                TpDistance = Mathf.Clamp(TpDistance / factor, 1.5f, 8f);
            }
            else if (Mode == CameraMode.TopDown)
            {
                TopDownDistance = Mathf.Clamp(TopDownDistance / factor, 3f, 10f);
            }
            else
            {
                Zoom = Mathf.Clamp(Zoom * factor, 0.7f, 2.6f);
            }
        }

        public void ResetView()
        {
            if (Mode == CameraMode.FirstPerson || Mode == CameraMode.ThirdPerson || Mode == CameraMode.TopDown)
            {
                ExitFirstPerson();
            }
            Yaw = 45f;
            Zoom = 1f;
            Pan = Vector3.zero;
        }

        public void FocusOn(Vector3 world, float amount)
        {
            if (Mode == CameraMode.FirstPerson || Mode == CameraMode.ThirdPerson || Mode == CameraMode.TopDown)
            {
                fpPosition = new Vector3(world.x, world.y + EyeHeight, world.z);
            }
            else
            {
                Pan = Vector3.Lerp(Vector3.zero, world - Target.center, amount);
                Pan.y = 0;
            }
        }

        // Stubs so GameManager compiles (cursor lock removed — no mouse look)
        public void LockCursor() { }
        public void UnlockCursor() { }

        Rect ScreenFrame()
        {
            if (Frame == null) return new Rect(0, 0, Screen.width, Screen.height);
            Frame.GetWorldCorners(corners);
            float xMin = corners[0].x, yMin = corners[0].y, xMax = corners[2].x, yMax = corners[2].y;
            if (xMax - xMin < 10 || yMax - yMin < 10) return new Rect(0, 0, Screen.width, Screen.height);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        void UpdateFirstPerson()
        {
            float dt = Time.unscaledDeltaTime;

            // Deselect any focused UI element so it doesn't eat keyboard
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                EventSystem.current.SetSelectedGameObject(null);

            // --- NO MOUSE LOOK — keyboard only ---
            // A/D and Left/Right = turn, W/S and Up/Down = forward/backward
            float turn = 0f;
            Vector3 move = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.z += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.z -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) turn -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) turn += 1f;
            }
#else
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.z -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turn -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turn += 1f;
#endif

            // Apply turning
            if (Mathf.Abs(turn) > 0.01f)
            {
                fpYaw += turn * TurnSpeed * dt;
            }

            // Apply movement (forward/backward only, in facing direction)
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

        void ComputeThirdPerson(out Vector3 pos, out Quaternion rot)
        {
            var lookTarget = fpPosition + Vector3.down * 0.4f;
            float elevation = 20f + fpPitch * 0.3f;
            var camDir = Quaternion.Euler(elevation, fpYaw + 180f, 0f) * Vector3.forward;
            var desired = lookTarget + camDir * TpDistance + Vector3.up * TpHeight;

            RaycastHit hit;
            float dist = TpDistance + TpHeight;
            if (Physics.SphereCast(lookTarget, 0.2f, (desired - lookTarget).normalized, out hit, dist))
            {
                if (hit.distance > 0.3f)
                    desired = lookTarget + (desired - lookTarget).normalized * (hit.distance - 0.2f);
                else
                    desired = lookTarget + (desired - lookTarget).normalized * 0.3f;
            }

            pos = desired;
            rot = Quaternion.LookRotation(lookTarget - desired);
        }

        void ComputeTopDown(out Vector3 pos, out Quaternion rot)
        {
            var lookTarget = fpPosition + Vector3.down * (EyeHeight - TopDownTargetHeight);

            float needed = TopDownMaxPitch;
            for (float p = TopDownPitch; p < TopDownMaxPitch; p += 6f)
            {
                var testDir = Quaternion.Euler(p, fpYaw, 0f) * Vector3.back;
                if (!Physics.SphereCast(lookTarget, 0.25f, testDir, out _, TopDownDistance, ~0, QueryTriggerInteraction.Ignore))
                {
                    needed = p;
                    break;
                }
            }

            float dt = Time.unscaledDeltaTime;
            if (tdPitch < 0f) tdPitch = needed;
            else if (needed > tdPitch) tdPitch = Mathf.Lerp(tdPitch, needed, Mathf.Clamp01(dt * 10f));
            else tdPitch = Mathf.Lerp(tdPitch, needed, Mathf.Clamp01(dt * 3f));

            rot = Quaternion.Euler(tdPitch, fpYaw, 0f);
            var dir = rot * Vector3.back;
            float dist = TopDownDistance;
            RaycastHit hit;
            if (Physics.SphereCast(lookTarget, 0.25f, dir, out hit, TopDownDistance, ~0, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(0.6f, hit.distance - 0.1f);
            pos = lookTarget + dir * dist;
        }

        void UpdatePlayerBody()
        {
            if (PlayerBody == null) return;
            bool visible = Mode == CameraMode.ThirdPerson || Mode == CameraMode.TopDown;
            PlayerBody.gameObject.SetActive(visible);
            if (visible)
            {
                PlayerBody.position = fpPosition + Vector3.down * EyeHeight;
                PlayerBody.rotation = Quaternion.Euler(0f, fpYaw, 0f);
            }
        }

        void LateUpdate()
        {
            if (Cam == null) return;

            if (transitioning)
            {
                transitionT += Time.unscaledDeltaTime / TransitionDuration;

                // Allow keyboard movement even during transition so player can move immediately
                if (Mode == CameraMode.FirstPerson || Mode == CameraMode.ThirdPerson || Mode == CameraMode.TopDown)
                    UpdateFirstPerson();

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
                else if (Mode == CameraMode.ThirdPerson)
                {
                    ComputeThirdPerson(out targetPos, out targetRot);
                    targetFov = 65f;
                }
                else if (Mode == CameraMode.TopDown)
                {
                    ComputeTopDown(out targetPos, out targetRot);
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

                if (transitionT >= 1f)
                {
                    transitioning = false;
                }
                return;
            }

            if (Mode == CameraMode.FirstPerson)
            {
                UpdateFirstPerson();
                var rot = Quaternion.Euler(fpPitch, fpYaw, 0f);
                Cam.transform.SetPositionAndRotation(fpPosition, rot);
            }
            else if (Mode == CameraMode.ThirdPerson)
            {
                UpdateFirstPerson(); // same WASD movement
                UpdatePlayerBody();
                Vector3 pos;
                Quaternion rot;
                ComputeThirdPerson(out pos, out rot);
                Cam.transform.SetPositionAndRotation(pos, rot);
            }
            else if (Mode == CameraMode.TopDown)
            {
                UpdateFirstPerson();
                UpdatePlayerBody();
                Vector3 pos;
                Quaternion rot;
                ComputeTopDown(out pos, out rot);
                Cam.transform.SetPositionAndRotation(pos, rot);
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
