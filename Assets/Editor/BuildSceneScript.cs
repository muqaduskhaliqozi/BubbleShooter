using UnityEditor;
using UnityEngine;
using BubbleShooter.Editor;

public class BuildSceneScript
{
    [MenuItem("Build/RunBuild")]
    public static void Run()
    {
        Debug.Log("--- RUN BUILD TRIGGERED ---");
        BuildGameplaySceneHierarchy.BuildScene();
    }
}
