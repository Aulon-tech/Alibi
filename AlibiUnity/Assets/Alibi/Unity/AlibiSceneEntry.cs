using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alibi.Unity
{
    public sealed class AlibiSceneEntry : MonoBehaviour
    {
        public bool gameScene;
        GameObject current;
        static AlibiSceneEntry instance;

        void Awake()
        {
            instance = this;
        }

        void Start()
        {
            Build(gameScene);
        }

        void Build(bool game)
        {
            if (current != null)
            {
                current.SetActive(false);
                Destroy(current);
            }
            current = new GameObject(game ? "AlibiGame" : "AlibiMenu");
            current.transform.SetParent(transform, false);
            AlibiFlow.EnsureEventSystem();
            if (game) current.AddComponent<GameManager>();
            else current.AddComponent<MainMenuController>();
        }

        public static void SwitchInPlace(bool game)
        {
            if (instance == null)
            {
                var go = new GameObject("Alibi");
                instance = go.AddComponent<AlibiSceneEntry>();
                instance.gameScene = game;
                return;
            }
            instance.gameScene = game;
            instance.Build(game);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (Finder.First<AlibiSceneEntry>() != null) return;
            var scene = SceneManager.GetActiveScene();
            bool fallback = scene.name == "SampleScene" || scene.name == "Untitled" || string.IsNullOrEmpty(scene.path) || scene.name.StartsWith("Alibi");
            if (!fallback) return;
            var go = new GameObject("Alibi");
            var entry = go.AddComponent<AlibiSceneEntry>();
            entry.gameScene = scene.name == AlibiFlow.GameScene;
        }
    }
}
