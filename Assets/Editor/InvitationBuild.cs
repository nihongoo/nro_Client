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
        const string output = "Build/invitation-p2a/NRO_INVITATION_P2A.apk";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        Debug.Log("Invitation APK: " + report.summary.result + ", errors=" + report.summary.totalErrors);
        File.WriteAllText("Build/invitation-p2a/result.txt", report.summary.result + ", errors=" + report.summary.totalErrors);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Invitation Android build failed: " + report.summary.result);
    }
}
