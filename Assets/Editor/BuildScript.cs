using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    public static void PerformBuild()
    {
        PlayerSettings.WebGL.template = "PROJECT:Pokesket";
        
        string buildPath = "Build/WebGL";
        string[] scenes = new string[] {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/CharacterSelection.unity",
            "Assets/Scenes/GameScene.unity"
        };

        BuildReport report = BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.WebGL, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException($"WebGL build failed with result {report.summary.result}. See the Unity build report for details.");
        }

        Debug.Log("Build WebGL terminé dans " + buildPath);
    }
}
