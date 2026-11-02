using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace Alibi.Unity
{
    public enum MarkerState { Hidden, Possible, Confirmed }

    public sealed class SuspectMarker : MonoBehaviour, IPointerClickHandler
    {
        public int Suspect;
        public int Room;
        public MarkerState State = MarkerState.Hidden;
        public bool Conflict;
        public VillaView View;

        Renderer body, head;
        GameObject ring, conflictRing;
        Material lit, ghost;
        float targetScale;
        float scale;

        public bool IsVisible { get { return State != MarkerState.Hidden || scale > 0.05f; } }
        public Vector3 HeadPoint { get { return transform.position + Vector3.up * 1.18f * Mathf.Max(scale, 0.01f); } }

        public static SuspectMarker Create(Transform parent, VillaView view, int suspect, int room, Vector3 pos, Material lit, Material ghost, Material ringMat, Material conflictMat)
        {
            var go = new GameObject("Marker_" + suspect + "_" + room);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var m = go.AddComponent<SuspectMarker>();
            m.Suspect = suspect;
            m.Room = room;
            m.View = view;
            m.lit = lit;
            m.ghost = ghost;
            var bodyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyGo.name = "Body";
            bodyGo.transform.SetParent(go.transform, false);
            bodyGo.transform.localPosition = new Vector3(0, 0.4f, 0);
            bodyGo.transform.localScale = new Vector3(0.36f, 0.4f, 0.36f);
            m.body = bodyGo.GetComponent<Renderer>();
            var headGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headGo.name = "Head";
            headGo.transform.SetParent(go.transform, false);
            headGo.transform.localPosition = new Vector3(0, 0.95f, 0);
            headGo.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);
            Object.Destroy(headGo.GetComponent<Collider>());
            m.head = headGo.GetComponent<Renderer>();
            m.ring = MakeRing(go.transform, ringMat, 0.78f, 0.02f);
            m.conflictRing = MakeRing(go.transform, conflictMat, 0.95f, 0.025f);
            m.ApplyLook();
            m.scale = 0f;
            m.targetScale = 0f;
            go.transform.localScale = Vector3.zero;
            return m;
        }

        static GameObject MakeRing(Transform parent, Material mat, float size, float y)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Ring";
            q.transform.SetParent(parent, false);
            q.transform.localPosition = new Vector3(0, y, 0);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(size, size, 1f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            Object.Destroy(q.GetComponent<Collider>());
            q.SetActive(false);
            return q;
        }

        public void SetState(MarkerState state, bool conflict)
        {
            State = state;
            Conflict = conflict && state != MarkerState.Hidden;
            ApplyLook();
            targetScale = state == MarkerState.Hidden ? 0f : state == MarkerState.Possible ? 0.94f : 1f;
            if (targetScale > 0 && !gameObject.activeSelf) gameObject.SetActive(true);
        }

        void ApplyLook()
        {
            bool confirmed = State == MarkerState.Confirmed;
            body.sharedMaterial = confirmed ? lit : ghost;
            head.sharedMaterial = confirmed ? lit : ghost;
            body.shadowCastingMode = confirmed ? ShadowCastingMode.On : ShadowCastingMode.Off;
            head.shadowCastingMode = confirmed ? ShadowCastingMode.On : ShadowCastingMode.Off;
            ring.SetActive(State == MarkerState.Possible);
            conflictRing.SetActive(Conflict);
        }

        void Update()
        {
            float speed = targetScale > scale ? 5f : 3.2f;
            scale = Mathf.MoveTowards(scale, targetScale, Time.deltaTime * speed);
            float eased = scale <= 0 ? 0 : Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, scale));
            transform.localScale = new Vector3(eased, eased, eased);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (View != null && View.SuspectClicked != null && State != MarkerState.Hidden) View.SuspectClicked(Suspect);
        }
    }
}
