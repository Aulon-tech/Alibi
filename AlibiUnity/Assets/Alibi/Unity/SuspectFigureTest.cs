using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;

namespace Alibi.Unity
{
    public class SuspectFigureTest : MonoBehaviour
    {
        readonly List<SuspectFigure> figures = new List<SuspectFigure>();
        GameObject marker;
        bool isTalking;
        float markerSpeed = 3.5f;

        void Start()
        {
            // 6 suspect definitions with distinct ColorHex values
            var suspects = new SuspectInfo[]
            {
                new SuspectInfo("A", "Arta", "Arta", "Artën", "kuzhiniere", "#3FA79A"),
                new SuspectInfo("B", "Blerimi", "Blerimi", "Blerimin", "shofer", "#D08B3A"),
                new SuspectInfo("D", "Drita", "Drita", "Dritën", "sekretare", "#A785E0"),
                new SuspectInfo("E", "Elmazi", "Elmazi", "Elmazin", "kopshtar", "#5B9BD5"),
                new SuspectInfo("F", "Fatmiri", "Fatmiri", "Fatmirin", "nipi i pronarit", "#D9667A"),
                new SuspectInfo("G", "Genta", "Genta", "Gentën", "ekonomiste", "#C4A35A")
            };

            float spacing = 0.55f;
            float startX = -(suspects.Length - 1) * spacing * 0.5f;

            var figuresRoot = new GameObject("FiguresRoot");
            figuresRoot.transform.SetParent(transform, false);

            for (int i = 0; i < suspects.Length; i++)
            {
                var fig = SuspectFigure.Build(figuresRoot.transform, suspects[i]);
                fig.transform.localPosition = new Vector3(startX + i * spacing, 0f, 0f);
                figures.Add(fig);
            }

            // Look target marker (sphere)
            marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "LookTargetMarker";
            marker.transform.position = new Vector3(0f, 0.25f, 1.8f);
            marker.transform.localScale = Vector3.one * 0.14f;

            var col = marker.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            var rend = marker.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader sh = null;
                if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                    sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
                var mat = new Material(sh);
                mat.color = new Color(1f, 0.85f, 0.2f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0.85f, 0.2f));
                rend.sharedMaterial = mat;
            }
        }

        void Update()
        {
            // Move marker with Arrow keys or WASD
            if (marker != null)
            {
                float dx = 0f;
                float dz = 0f;

                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dx -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dx += 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dz += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dz -= 1f;

                if (dx != 0f || dz != 0f)
                {
                    Vector3 move = new Vector3(dx, 0f, dz).normalized * (markerSpeed * Time.deltaTime);
                    marker.transform.position += move;
                }
            }

            // T toggles SetTalking on all figures
            if (Input.GetKeyDown(KeyCode.T))
            {
                isTalking = !isTalking;
                for (int i = 0; i < figures.Count; i++)
                {
                    figures[i].SetTalking(isTalking);
                }
            }

            // L calls LookAt on all figures with marker position
            if (Input.GetKeyDown(KeyCode.L) || Input.GetKey(KeyCode.L))
            {
                if (marker != null)
                {
                    Vector3 pos = marker.transform.position;
                    for (int i = 0; i < figures.Count; i++)
                    {
                        figures[i].LookAt(pos);
                    }
                }
            }
        }

        void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 340, 115), "SuspectFigure Test");
            GUI.Label(new Rect(22, 35, 320, 20), "WASD / Arrow Keys : Move look target marker");
            GUI.Label(new Rect(22, 55, 320, 20), "L : Call LookAt(marker.position)");
            GUI.Label(new Rect(22, 75, 320, 20), "T : Toggle SetTalking [Currently: " + (isTalking ? "TALKING" : "IDLE") + "]");
            GUI.Label(new Rect(22, 95, 320, 20), "Marker: " + (marker != null ? marker.transform.position.ToString("F2") : "None"));
        }
    }
}
