using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// CLI: Unity -batchmode -quit -projectPath <repo> -executeMethod InvitationBuild.BuildAndroid
public static class InvitationBuild
{
    [MenuItem("Tools/Invitations/Build Android")]
    public static void BuildAndroid()
    {
        BuildAndroidTo("Build/invitation-p2a/NRO_INVITATION_P2A.apk");
    }

    [MenuItem("Tools/World chat/Build Android")]
    public static void BuildWorldChatAndroid()
    {
        BuildAndroidTo("Build/world-chat-p3/NRO_WORLD_CHAT_P3.apk");
    }

    private static void BuildAndroidTo(string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        Debug.Log("APK " + output + ": " + report.summary.result + ", errors=" + report.summary.totalErrors);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "result.txt"), report.summary.result + ", errors=" + report.summary.totalErrors);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Android build failed: " + report.summary.result);
    }
}
