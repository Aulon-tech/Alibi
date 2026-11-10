using Alibi.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alibi.Unity
{
    public class SuspectFigure : MonoBehaviour
    {
        // Public API
        public Transform HeadTop { get { return headTop; } }

        Transform headTop;
        Transform headPivot;
        Transform bodyTransform;
        Transform leftArmPivot;
        Transform rightArmPivot;

        Vector3 baseBodyScale;
        Vector3 baseHeadPivotPos;
        Vector3 baseLeftArmPivotPos;
        Vector3 baseRightArmPivotPos;

        float phaseOffset;
        bool isTalking;
        float talkingWeight;

        bool hasTargetRotation;
        Quaternion targetRotation;

        // Shared materials cache
        static Shader s_cachedShader;
        static Material s_sharedSkinMat;
        static Material s_sharedHairMat;
        static Material s_sharedLegsMat;

        const float TurnSpeed = 3.5f;
        const float TalkBlendSpeed = 4.5f;

        void Awake()
        {
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        public static SuspectFigure Build(Transform parent, SuspectInfo info)
        {
            string name = info != null && !string.IsNullOrEmpty(info.Name) ? "SuspectFigure_" + info.Name : "SuspectFigure";
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var figure = go.AddComponent<SuspectFigure>();

            // Parse main color
            Color mainColor = new Color(0.28f, 0.52f, 0.82f); // fallback color
            if (info != null && !string.IsNullOrEmpty(info.ColorHex))
            {
                Color parsed;
                if (ColorUtility.TryParseHtmlString(info.ColorHex, out parsed))
                {
                    mainColor = parsed;
                }
            }

            // Material colors set per figure instance for main color (body & arms)
            var figureMainMat = CreateOpaqueMaterial(mainColor, 0.2f);
            var skinMat = GetSkinMaterial();
            var hairMat = GetHairMaterial();
            var legsMat = GetLegsMaterial();

            // 1. Legs (cubes from y=0 to y=0.13)
            var legScale = new Vector3(0.036f, 0.13f, 0.036f);
            CreatePrimitivePart(PrimitiveType.Cube, "LeftLeg", go.transform, new Vector3(-0.045f, 0.065f, 0f), legScale, legsMat);
            CreatePrimitivePart(PrimitiveType.Cube, "RightLeg", go.transform, new Vector3(0.045f, 0.065f, 0f), legScale, legsMat);

            // 2. Body (capsule from y=0.13 to y=0.30, height 0.17)
            var bodyGo = CreatePrimitivePart(PrimitiveType.Capsule, "Body", go.transform, new Vector3(0f, 0.215f, 0f), new Vector3(0.125f, 0.085f, 0.08f), figureMainMat);
            figure.bodyTransform = bodyGo.transform;
            figure.baseBodyScale = figure.bodyTransform.localScale;

            // 3. Neck & Head Pivot (at y=0.295f)
            var headPivotGo = new GameObject("HeadPivot");
            headPivotGo.transform.SetParent(go.transform, false);
            headPivotGo.transform.localPosition = new Vector3(0f, 0.295f, 0f);
            figure.headPivot = headPivotGo.transform;
            figure.baseHeadPivotPos = figure.headPivot.localPosition;

            // Head (sphere, diameter ~0.11f)
            CreatePrimitivePart(PrimitiveType.Sphere, "Head", figure.headPivot, new Vector3(0f, 0.06f, 0f), new Vector3(0.11f, 0.11f, 0.11f), skinMat);

            // Hair (small cube on top of the head)
            CreatePrimitivePart(PrimitiveType.Cube, "Hair", figure.headPivot, new Vector3(0f, 0.115f, -0.005f), new Vector3(0.108f, 0.04f, 0.108f), hairMat);

            // HeadTop (empty child positioned slightly above the head)
            var headTopGo = new GameObject("HeadTop");
            headTopGo.transform.SetParent(figure.headPivot, false);
            headTopGo.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            figure.headTop = headTopGo.transform;

            // 4. Arms & Arm Pivots (shoulders at y=0.27f, arms hanging down)
            var leftPivotGo = new GameObject("LeftArmPivot");
            leftPivotGo.transform.SetParent(go.transform, false);
            leftPivotGo.transform.localPosition = new Vector3(-0.082f, 0.27f, 0f);
            figure.leftArmPivot = leftPivotGo.transform;
            figure.baseLeftArmPivotPos = figure.leftArmPivot.localPosition;

            var rightPivotGo = new GameObject("RightArmPivot");
            rightPivotGo.transform.SetParent(go.transform, false);
            rightPivotGo.transform.localPosition = new Vector3(0.082f, 0.27f, 0f);
            figure.rightArmPivot = rightPivotGo.transform;
            figure.baseRightArmPivotPos = figure.rightArmPivot.localPosition;

            var armScale = new Vector3(0.034f, 0.06f, 0.034f);
            CreatePrimitivePart(PrimitiveType.Capsule, "LeftArmMesh", figure.leftArmPivot, new Vector3(0f, -0.06f, 0f), armScale, figureMainMat);
            CreatePrimitivePart(PrimitiveType.Capsule, "RightArmMesh", figure.rightArmPivot, new Vector3(0f, -0.06f, 0f), armScale, figureMainMat);

            return figure;
        }

        public void SetTalking(bool on)
        {
            isTalking = on;
        }

        public void LookAt(Vector3 target)
        {
            Vector3 dir = target - transform.position;
            dir.y = 0f; // rotate around Y axis only
            if (dir.sqrMagnitude > 0.0001f)
            {
                targetRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                hasTargetRotation = true;
            }
        }

        void Update()
        {
            // 1. Idle breathing (subtle Y scale pulse of body via Mathf.Sin)
            float breath = Mathf.Sin(Time.time * 2.2f + phaseOffset);
            if (bodyTransform != null)
            {
                float scaleY = baseBodyScale.y * (1f + breath * 0.035f);
                bodyTransform.localScale = new Vector3(baseBodyScale.x, scaleY, baseBodyScale.z);
            }

            // Subtle vertical displacement of head and arms following breath
            float breathY = breath * 0.0025f;
            if (headPivot != null) headPivot.localPosition = baseHeadPivotPos + new Vector3(0f, breathY, 0f);
            if (leftArmPivot != null) leftArmPivot.localPosition = baseLeftArmPivotPos + new Vector3(0f, breathY, 0f);
            if (rightArmPivot != null) rightArmPivot.localPosition = baseRightArmPivotPos + new Vector3(0f, breathY, 0f);

            // 2. Talking and Idle motion blend
            float targetWeight = isTalking ? 1f : 0f;
            talkingWeight = Mathf.MoveTowards(talkingWeight, targetWeight, Time.deltaTime * TalkBlendSpeed);

            // Head motions: idle sway vs talking nod
            Quaternion idleHeadRot = Quaternion.Euler(
                Mathf.Sin(Time.time * 1.4f + phaseOffset) * 2f,
                Mathf.Cos(Time.time * 0.9f + phaseOffset) * 3f,
                Mathf.Sin(Time.time * 1.1f + phaseOffset) * 2f
            );

            Quaternion talkHeadRot = Quaternion.Euler(
                Mathf.Sin(Time.time * 8.5f + phaseOffset) * 8.5f,
                Mathf.Sin(Time.time * 4f + phaseOffset * 1.3f) * 3.5f,
                0f
            );

            if (headPivot != null)
            {
                headPivot.localRotation = Quaternion.Slerp(idleHeadRot, talkHeadRot, talkingWeight);
            }

            // Arm motions: idle subtle hang vs talking swing / gestures
            Quaternion idleLeftArm = Quaternion.Euler(Mathf.Sin(Time.time * 1.2f + phaseOffset) * 1.5f, 0f, 2f);
            Quaternion idleRightArm = Quaternion.Euler(Mathf.Sin(Time.time * 1.2f + phaseOffset) * 1.5f, 0f, -2f);

            float talkLeftPitch = 15f + Mathf.Sin(Time.time * 6.5f + phaseOffset) * 20f;
            float talkRightPitch = 15f + Mathf.Cos(Time.time * 6.5f + phaseOffset) * 20f;

            Quaternion talkLeftArm = Quaternion.Euler(talkLeftPitch, 0f, 5f);
            Quaternion talkRightArm = Quaternion.Euler(talkRightPitch, 0f, -5f);

            if (leftArmPivot != null)
            {
                leftArmPivot.localRotation = Quaternion.Slerp(idleLeftArm, talkLeftArm, talkingWeight);
            }
            if (rightArmPivot != null)
            {
                rightArmPivot.localRotation = Quaternion.Slerp(idleRightArm, talkRightArm, talkingWeight);
            }

            // 3. Smooth Y-axis rotation toward target
            if (hasTargetRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * TurnSpeed);
            }
        }

        static GameObject CreatePrimitivePart(PrimitiveType type, string partName, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = localScale;

            // Remove collider unless needed
            var col = part.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }

            var rend = part.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = mat;
            }
            return part;
        }

        static Shader FindOpaqueShader()
        {
            if (s_cachedShader != null) return s_cachedShader;
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                s_cachedShader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (s_cachedShader == null)
            {
                s_cachedShader = Shader.Find("Standard");
            }
            if (s_cachedShader == null)
            {
                s_cachedShader = Shader.Find("Legacy Shaders/Diffuse");
            }
            return s_cachedShader;
        }

        static Material CreateOpaqueMaterial(Color color, float smoothness)
        {
            var shader = FindOpaqueShader();
            var mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            mat.renderQueue = (int)RenderQueue.Geometry;
            return mat;
        }

        static Material GetSkinMaterial()
        {
            if (s_sharedSkinMat == null)
                s_sharedSkinMat = CreateOpaqueMaterial(new Color(0.92f, 0.76f, 0.65f), 0.1f);
            return s_sharedSkinMat;
        }

        static Material GetHairMaterial()
        {
            if (s_sharedHairMat == null)
                s_sharedHairMat = CreateOpaqueMaterial(new Color(0.18f, 0.14f, 0.11f), 0.15f);
            return s_sharedHairMat;
        }

        static Material GetLegsMaterial()
        {
            if (s_sharedLegsMat == null)
                s_sharedLegsMat = CreateOpaqueMaterial(new Color(0.18f, 0.20f, 0.26f), 0.15f);
            return s_sharedLegsMat;
        }
    }
}
