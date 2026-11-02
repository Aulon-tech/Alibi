using System.Collections.Generic;
using System.IO;
using Alibi.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Alibi.EditorTools
{
    [InitializeOnLoad]
    public static class AlibiSetup
    {
        const string Root = "Assets/Alibi";
        const string ScenesDir = "Assets/Alibi/Scenes";
        const string MenuPath = "Assets/Alibi/Scenes/Alibi_MainMenu.unity";
        const string GamePath = "Assets/Alibi/Scenes/Alibi_Game.unity";
        const string MaterialPath = "Assets/Alibi/Resources/AlibiLit.mat";
        const string PromptKey = "Alibi.SetupPrompted";

        static AlibiSetup()
        {
            EditorApplication.delayCall += FirstRun;
        }

        static void FirstRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EnsureMaterial();
            string key = PromptKey + "." + Application.dataPath;
            if (File.Exists(MenuPath) || EditorPrefs.GetBool(key, false)) return;
            EditorPrefs.SetBool(key, true);
            bool yes = EditorUtility.DisplayDialog(
                "Alibi",
                "Paketa Alibi u importua.\n\nTë krijohen skenat Alibi_MainMenu dhe Alibi_Game, të shtohen te Build Settings dhe të hapet menyja?\n\nPastaj shtyp Play.",
                "Po, krijoji",
                "Më vonë");
            if (yes) SetupScenes(true);
        }

        [MenuItem("Alibi/Luaj demon", false, 1)]
        public static void PlayDemo()
        {
            if (EditorApplication.isPlaying) return;
            if (!SetupScenes(true)) return;
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Alibi/Krijo skenat dhe Build Settings", false, 2)]
        public static void CreateScenes()
        {
            SetupScenes(true);
        }

        [MenuItem("Alibi/Rifillo progresin e kartave", false, 20)]
        public static void ResetProgress()
        {
            AlibiFlow.ResetProgress();
            Debug.Log("Alibi: progresi u rifillua.");
        }

        static bool SetupScenes(bool openMenu)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            EnsureMaterial();
            if (!AssetDatabase.IsValidFolder(ScenesDir)) AssetDatabase.CreateFolder(Root, "Scenes");
            if (!File.Exists(GamePath)) CreateScene(GamePath, true);
            if (!File.Exists(MenuPath)) CreateScene(MenuPath, false);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == MenuPath || s.path == GamePath);
            scenes.Insert(0, new EditorBuildSettingsScene(MenuPath, true));
            scenes.Insert(1, new EditorBuildSettingsScene(GamePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            if (openMenu) EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            return true;
        }

        static void CreateScene(string path, bool game)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Alibi");
            var entry = go.AddComponent<AlibiSceneEntry>();
            entry.gameScene = game;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        static void EnsureMaterial()
        {
            Shader sh = null;
            if (GraphicsSettings.currentRenderPipeline != null) sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null) return;
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
            {
                if (existing.shader != sh)
                {
                    existing.shader = sh;
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssets();
                }
                return;
            }
            if (!AssetDatabase.IsValidFolder(Root + "/Resources")) AssetDatabase.CreateFolder(Root, "Resources");
            var mat = new Material(sh);
            AssetDatabase.CreateAsset(mat, MaterialPath);
            AssetDatabase.SaveAssets();
        }
    }
}
