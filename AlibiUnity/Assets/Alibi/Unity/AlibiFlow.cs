using System;
using Alibi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public static class Finder
    {
        public static T First<T>() where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<T>();
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        public static T[] All<T>() where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return UnityEngine.Object.FindObjectsOfType<T>();
#endif
        }
    }

    public static class AlibiFlow
    {
        public const string MenuScene = "Alibi_MainMenu";
        public const string GameScene = "Alibi_Game";

        public static CaseDefinition PendingCase;
        public static string MenuNote;
        public static bool TutorialEnabled = true;

        public static void StartCase(CaseDefinition c)
        {
            PendingCase = c;
            Load(GameScene, true);
        }

        public static void ToMenu(string note)
        {
            MenuNote = note;
            Load(MenuScene, false);
        }

        static void Load(string scene, bool game)
        {
            if (Application.CanStreamedLevelBeLoaded(scene))
            {
                SceneManager.LoadScene(scene);
                return;
            }
            AlibiSceneEntry.SwitchInPlace(game);
        }

        public static int BestStars(int card)
        {
            return PlayerPrefs.GetInt("alibi_card_" + card + "_stars", 0);
        }

        public static bool CardWon(int card)
        {
            return PlayerPrefs.GetInt("alibi_card_" + card + "_won", 0) == 1;
        }

        public static void RecordWin(int card, int stars)
        {
            if (card <= 0) return;
            PlayerPrefs.SetInt("alibi_card_" + card + "_won", 1);
            if (stars > BestStars(card)) PlayerPrefs.SetInt("alibi_card_" + card + "_stars", stars);
            PlayerPrefs.Save();
        }

        public static bool UnlockAll
        {
            get { return PlayerPrefs.GetInt("alibi_unlock_all", 0) == 1; }
            set
            {
                PlayerPrefs.SetInt("alibi_unlock_all", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool CardUnlocked(int card)
        {
            if (card <= 1 || UnlockAll) return true;
            return CardWon(card - 1);
        }

        public static int NextCard()
        {
            for (int k = 1; k <= 3; k++) if (!CardWon(k) && CardUnlocked(k)) return k;
            return 3;
        }

        public static void ResetProgress()
        {
            for (int k = 1; k <= 3; k++)
            {
                PlayerPrefs.DeleteKey("alibi_card_" + k + "_won");
                PlayerPrefs.DeleteKey("alibi_card_" + k + "_stars");
            }
            PlayerPrefs.DeleteKey("alibi_unlock_all");
            PlayerPrefs.Save();
        }

        public static Canvas CreateCanvas(Transform parent, string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var existing = Finder.First<EventSystem>();
            if (existing != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<StandaloneInputModule>();
#else
            var t = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (t != null)
            {
                var module = go.AddComponent(t);
                var assign = t.GetMethod("AssignDefaultActions");
                if (assign != null) assign.Invoke(module, null);
            }
            else
            {
                Debug.LogWarning("Alibi: nuk u gjet asnje modul inputi per UI. Instalo Input System ose aktivizo Input Manager (Old).");
            }
#endif
        }

        public static void DisableForeignCamerasAndLights(Transform owner)
        {
            foreach (var cam in Finder.All<Camera>())
            {
                if (!cam.transform.IsChildOf(owner)) cam.enabled = false;
            }
            foreach (var light in Finder.All<Light>())
            {
                if (!light.transform.IsChildOf(owner)) light.enabled = false;
            }
        }
    }
}
