#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

internal static class GameDuoIosPostBuild
{
    [PostProcessBuild(100)]
    private static void ConfigureLocalNetwork(BuildTarget target, string outputPath)
    {
        if (target != BuildTarget.iOS)
            return;

        string path = Path.Combine(outputPath, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(path);
        plist.root.SetString("NSLocalNetworkUsageDescription",
            "Siempre Juntos usa la red local para jugar con otra persona conectada a la misma red Wi-Fi.");
        plist.WriteToFile(path);
    }
}
#endif
