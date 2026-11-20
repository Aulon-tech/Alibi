using UnityEngine;
using UnityEngine.Rendering;

namespace Alibi.Unity
{
    public static class SceneSetup
    {
        public static void Lights(Transform parent, Bounds villa)
        {
            // Moonlight - main directional light (night scene)
            var sunGo = new GameObject("Moon");
            sunGo.transform.SetParent(parent, false);
            sunGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.55f, 0.6f, 0.75f);
            sun.intensity = 0.4f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.6f;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.3f;

            // Warm fill light from above the villa (simulates interior glow)
            var fillGo = new GameObject("FillLight");
            fillGo.transform.SetParent(parent, false);
            fillGo.transform.position = villa.center + new Vector3(0, 12f, 0);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(1f, 0.85f, 0.65f);
            fill.range = 50f;
            fill.intensity = 1.8f;
            fill.shadows = LightShadows.None;

            // Exterior ambient lights at the villa edges
            float dx = villa.extents.x * 0.8f;
            float dz = villa.extents.z * 0.8f;
            AddAmbientLight(parent, villa.center + new Vector3(dx, 6f, dz), 30f, 0.6f);
            AddAmbientLight(parent, villa.center + new Vector3(-dx, 6f, -dz), 30f, 0.6f);

            // Dark atmospheric ambient
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.13f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.08f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 120f;
        }

        static void AddAmbientLight(Transform parent, Vector3 pos, float range, float intensity)
        {
            var go = new GameObject("AmbientLight");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.7f, 0.75f, 0.9f);
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
        }
    }
}
