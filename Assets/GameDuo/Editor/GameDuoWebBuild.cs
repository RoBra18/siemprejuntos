using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class GameDuoWebBuild
{
    public static void Build()
    {
        var scenes = new[] { CoopBeam.GameDuoMainMenu.ScenePath }
            .Concat(CoopBeam.GameDuoCampaign.Scenes).ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No enabled build scenes.");
        // Fallback allows static hosting without custom compression response headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("WebGL build failed: " + report.summary.result);
        var moviesDirectory = Path.Combine("Builds/WebGL", "StreamingAssets/GameDuoMovies");
        Directory.CreateDirectory(moviesDirectory);
        foreach (var movie in Directory.GetFiles("Assets/GameDuo/video", "*.mp4"))
            File.Copy(movie, Path.Combine(moviesDirectory, Path.GetFileName(movie)), true);
        UnityEngine.Debug.Log("WEB_BUILD_SUCCEEDED: " + report.summary.totalSize + " bytes");
    }
}
