using UnityEngine;
using UnityEngine.Rendering;

namespace Alibi.Unity
{
    public static class SceneSetup
    {
        public static void Lights(Transform parent, Bounds villa)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(parent, false);
            sunGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.9f, 0.76f);
            sun.intensity = 1.0f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;

            var lampGo = new GameObject("Lamp");
            lampGo.transform.SetParent(parent, false);
            lampGo.transform.position = villa.center + new Vector3(0, 5.5f, 0);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.82f, 0.58f);
            lamp.range = 16f;
            lamp.intensity = 1.3f;
            lamp.shadows = LightShadows.None;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.27f, 0.34f);
            RenderSettings.fog = false;
        }
    }
}
