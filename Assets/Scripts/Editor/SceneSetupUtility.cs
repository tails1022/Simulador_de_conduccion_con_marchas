using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GyroPilot.UI;

namespace GyroPilot.Editor
{
    public static class SceneSetupUtility
    {
        [MenuItem("GyroPilot/Configurar Escenas y Build")]
        public static void SetupMainMenuAndBuildSettings()
        {
            string mainMenuPath = "Assets/Scenes/MainMenu.unity";
            string sampleScenePath = "Assets/Scenes/SampleScene.unity";

            // Crear escena MainMenu limpia
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Cámara Principal
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.10f);
            camObj.AddComponent<AudioListener>();

            // 2. Manejador del Menú de Inicio
            GameObject menuObj = new GameObject("MainMenuManager");
            menuObj.AddComponent<MainMenuManager>();

            // 3. Luz Direccional suave
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Guardar escena
            EditorSceneManager.SaveScene(scene, mainMenuPath);

            // Configurar EditorBuildSettings:
            // Índice 0: MainMenu.unity
            // Índice 1: SampleScene.unity
            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(mainMenuPath, true),
                new EditorBuildSettingsScene(sampleScenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes;

            Debug.Log("GyroPilot: Escenas configuradas con éxito en Build Settings (0: MainMenu, 1: SampleScene)");
        }
    }
}
