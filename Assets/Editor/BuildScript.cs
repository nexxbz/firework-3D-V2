using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildScript
{
    static string Arg(string name)
    {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
        return null;
    }

    static void AlwaysInclude(params string[] names)
    {
        var gs = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        if (gs == null) return;
        var so = new SerializedObject(gs);
        var arr = so.FindProperty("m_AlwaysIncludedShaders");
        foreach (var n in names)
        {
            var sh = Shader.Find(n);
            if (sh == null) continue;
            bool has = false;
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) has = true;
            if (has) continue;
            arr.InsertArrayElementAtIndex(arr.arraySize);
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
        }
        so.ApplyModifiedProperties();
    }

    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var sc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(sc, "Assets/Scenes/Main.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };

        AlwaysInclude("Standard", "Sprites/Default", "Legacy Shaders/Particles/Additive");

        PlayerSettings.companyName = "Demo";
        PlayerSettings.productName = "Fireworks Mini Unity";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.demo.fireworksmini");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;

        var dir = Arg("-customBuildPath") ?? "build/Android";
        var name = Arg("-customBuildName") ?? "FireworksMini";
        Directory.CreateDirectory(dir);

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = Path.Combine(dir, name + ".apk"),
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
