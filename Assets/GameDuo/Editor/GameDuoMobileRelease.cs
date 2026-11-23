using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using CoopBeam;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Headless release checks: unity -batchmode -quit -projectPath ... -executeMethod GameDuoMobileRelease.Validate
public static class GameDuoMobileRelease {
 const string ProductName="Siempre Juntos";
 const string Package="com.rogerfarfan.siemprejuntos";
 const string OutputRoot="Builds/Android";
 static void Require(bool condition,string message){if(!condition)throw new Exception("MOBILE_QA_FAILED: "+message);Debug.Log("MOBILE_QA_OK: "+message);}
 [MenuItem("Tools/DimDam/Validar móvil y multijugador")]
 public static void Validate(){
  Require(File.Exists(GameDuoMainMenu.ScenePath),"menu scene present");
  Require(GameDuoCampaign.Scenes.Length>0,"campaign scenes present");
  foreach(var path in GameDuoCampaign.Scenes){Require(File.Exists(path),"scene "+path);var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{if(path==GameDuoCampaign.Scenes[3])Require(FindInScene<IcePenguinRound>(scene),"penguin rescue round in "+scene.name);else{var game=FindInScene<GameManager>(scene);Require(game&&game.players!=null&&game.players.Length==2&&game.players[0]&&game.players[1],"two players in "+scene.name);}}finally{EditorSceneManager.CloseScene(scene,true);}}
  var menu=EditorSceneManager.OpenScene(GameDuoMainMenu.ScenePath,OpenSceneMode.Additive);
  try{Require(FindInScene<GameDuoMainMenu>(menu),"multiplayer menu component present");}finally{EditorSceneManager.CloseScene(menu,true);}
  CheckTouchMapping();
  CheckDanceDirections();
  try{CheckHostTransport();CheckGuestTransport();}
  finally{GameDuoLan.Local();if(GameDuoLan.Instance)UnityEngine.Object.DestroyImmediate(GameDuoLan.Instance.gameObject);}
  Debug.Log("MOBILE_QA_COMPLETE");
 }
 static T FindInScene<T>(Scene scene) where T:Component{foreach(var root in scene.GetRootGameObjects()){var match=root.GetComponentInChildren<T>(true);if(match)return match;}return null;}
 static void CheckTouchMapping(){
  var go=new GameObject("Mobile input QA");
  var input=go.AddComponent<GameDuoMobileInput>();
  var type=typeof(GameDuoMobileInput);
  const BindingFlags methods=BindingFlags.Instance|BindingFlags.NonPublic;
  var instance=type.GetField("instance",BindingFlags.Static|BindingFlags.NonPublic);
  var original=instance.GetValue(null);
  var layout=type.GetMethod("GetLayout",BindingFlags.Static|BindingFlags.NonPublic);
  var begin=type.GetMethod("BeginInputFrame",methods);
  var end=type.GetMethod("EndInputFrame",methods);
  var track=type.GetMethod("TrackPointer",methods);
  var reset=type.GetMethod("ResetInput",methods);
  Vector2 stick0=default,super0=default,stick1=default,super1=default;
  void Layout(GameDuoLan.LanMode mode,float left,float right,float bottom){
   var args=new object[]{mode,left,right,bottom,null,null,null,null};
   layout.Invoke(null,args);
   stick0=(Vector2)args[4];super0=(Vector2)args[5];stick1=(Vector2)args[6];super1=(Vector2)args[7];
  }
  void Track(int pointer,bool pressed,Vector2 position,GameDuoLan.LanMode mode=GameDuoLan.LanMode.Local){
   track.Invoke(input,new object[]{pointer,pressed,position,mode,stick0,super0,stick1,super1});
  }
  instance.SetValue(null,input);
  try{
   foreach(float width in new[]{640f,720f,960f,1200f}){
    Layout(GameDuoLan.LanMode.Local,42f,width-22f,522f);
    Require(Vector2.Distance(stick0,super0)>110f&&Vector2.Distance(stick1,super1)>110f&&
     Vector2.Distance(super0,super1)>84f&&Vector2.Distance(stick0,stick1)>136f&&
     Vector2.Distance(stick0,super1)>110f&&Vector2.Distance(stick1,super0)>110f,
     $"local touch areas remain separate at width {width}");
   }
   Layout(GameDuoLan.LanMode.Local,0f,960f,540f);
   begin.Invoke(input,null);
   Track(0,true,stick0+Vector2.right*45f);
   Track(1,true,stick1+Vector2.left*45f);
   end.Invoke(input,null);
   Require(GameDuoMobileInput.Get(0).axis>.9f&&GameDuoMobileInput.Get(1).axis<-.9f,"both local joysticks have independent axes");

   begin.Invoke(input,null);
   Track(0,false,super0);
   Track(1,false,super0);
   end.Invoke(input,null);
   Require(GameDuoMobileInput.Get(0).axis>.5f&&GameDuoMobileInput.Get(1).axis<-.9f&&
    !GameDuoMobileInput.Get(0).ultimate&&!GameDuoMobileInput.Get(1).ultimate,
    "sticks retain ownership when dragged into the super or across the screen");

   begin.Invoke(input,null);
   Track(0,false,stick0+Vector2.up*45f);
   Track(1,false,stick1+Vector2.left*45f);
   Track(2,true,super0);
   Track(3,true,super1);
   end.Invoke(input,null);
   Require(GameDuoMobileInput.Get(0).downPressed&&GameDuoMobileInput.Get(0).ultimatePressed&&
    GameDuoMobileInput.Get(1).axis<-.9f&&GameDuoMobileInput.Get(1).ultimatePressed,
    "both players can press super while holding their sticks");

   begin.Invoke(input,null);
   Track(2,false,stick0);
   Track(4,false,super1);
   Track(5,true,new Vector2(634f,300f));
   end.Invoke(input,null);
   Require(GameDuoMobileInput.Get(0).axis==0f&&GameDuoMobileInput.Get(0).ultimate&&
    !GameDuoMobileInput.Get(0).ultimatePressed&&GameDuoMobileInput.Get(1).axis==0f&&
    !GameDuoMobileInput.Get(1).ultimate,"released sticks clear, super holds without repeating, and stray touches stay inactive");

   foreach(var mode in new[]{GameDuoLan.LanMode.Host,GameDuoLan.LanMode.Guest}){
    reset.Invoke(input,null);
    Layout(mode,42f,680f,522f);
    begin.Invoke(input,null);
    Track(0,true,stick0+Vector2.right*45f,mode);
    Track(1,true,super0,mode);
    end.Invoke(input,null);
    int player=mode==GameDuoLan.LanMode.Host?0:1;
    Require(GameDuoMobileInput.Get(player).axis>.9f&&GameDuoMobileInput.Get(player).ultimatePressed&&
     GameDuoMobileInput.Get(1-player).axis==0f&&!GameDuoMobileInput.Get(1-player).ultimate,
     $"{mode} local touches belong only to its assigned player");
   }
  }finally{
   instance.SetValue(null,original);
   UnityEngine.Object.DestroyImmediate(go);
  }
 }
 static void CheckDanceDirections(){
  var direction=typeof(CoopInteractionDirector).GetMethod("Direction",BindingFlags.Static|BindingFlags.NonPublic);
  int Read(GameDuoMobileInput.State state)=>(int)direction.Invoke(null,new object[]{state});
  Require(Read(default)==-1&&Read(new GameDuoMobileInput.State{axis=.2f})==-1,"dance ignores neutral and short lateral movements");
  Require(Read(new GameDuoMobileInput.State{jump=true})==0&&Read(new GameDuoMobileInput.State{down=true})==2,"dance recognizes vertical stick directions");
  Require(Read(new GameDuoMobileInput.State{axis=-.8f})==1&&Read(new GameDuoMobileInput.State{axis=.8f})==3,"dance recognizes horizontal stick directions");
  Require(Read(new GameDuoMobileInput.State{axis=.99f,jump=true})==3&&Read(new GameDuoMobileInput.State{axis=.65f,down=true})==2,
   "dance resolves diagonal input to a single direction");
 }
 static void CheckHostTransport(){
  GameDuoLan.StartHost();
  try{using(var client=Connect(3000)){var bytes=Encoding.ASCII.GetBytes("0.75,1,0,1,0,0\n");client.GetStream().Write(bytes,0,bytes.Length);bool received=SpinWait.SpinUntil(()=>GameDuoLan.Remote.axis>.7f&&GameDuoLan.Remote.jump&&GameDuoLan.Remote.ultimate,3000);Require(received,"host receives guest controls over TCP");}}
  finally{GameDuoLan.Local();}
 }
 static void CheckGuestTransport(){
  var listener=new TcpListener(IPAddress.Loopback,GameDuoLan.Port);
  try{
   listener.Start();GameDuoLan.Join("127.0.0.1");
   try{Require(SpinWait.SpinUntil(()=>listener.Pending(),3000),"guest connects to host");using(var client=listener.AcceptTcpClient()){client.ReceiveTimeout=3000;GameDuoLan.Send(new GameDuoMobileInput.State{axis=-.5f,jump=true,ultimate=true});using(var reader=new StreamReader(client.GetStream(),Encoding.ASCII)){bool found=false;var end=DateTime.UtcNow.AddSeconds(3);while(DateTime.UtcNow<end){var line=reader.ReadLine();if(line!=null&&line.StartsWith("-0.50,1,0,1,0,0",StringComparison.Ordinal)){found=true;break;}}Require(found,"guest sends joystick and buttons over TCP");}}}
   finally{GameDuoLan.Local();}
  }finally{listener.Stop();}
 }
 static TcpClient Connect(int timeoutMs){var client=new TcpClient();var ar=client.BeginConnect(IPAddress.Loopback,GameDuoLan.Port,null,null);if(!ar.AsyncWaitHandle.WaitOne(timeoutMs)){client.Close();throw new Exception("MOBILE_QA_FAILED: host socket timeout");}client.EndConnect(ar);return client;}
 [MenuItem("Tools/DimDam/Crear APK de prueba")]
 public static void BuildTestApk(){
  if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android)){
   EditorUtility.DisplayDialog("Android Build Support","Instala Android Build Support, SDK y NDK, y OpenJDK desde Unity Hub para esta versión de Unity.","Aceptar");
   return;
  }
  string output=EditorUtility.SaveFilePanel("Guardar APK de prueba",Path.GetFullPath("."),"DimDam-prueba","apk");
  if(string.IsNullOrEmpty(output))return;

  bool appBundle=EditorUserBuildSettings.buildAppBundle;
  bool exportProject=EditorUserBuildSettings.exportAsGoogleAndroidProject;
  bool customKeystore=PlayerSettings.Android.useCustomKeystore;
  try{
   EditorUserBuildSettings.buildAppBundle=false;
   EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
   PlayerSettings.Android.useCustomKeystore=false;
   PrepareAndroidMovies();
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
    scenes=GameDuoCampaignBuildScenes(),locationPathName=output,target=BuildTarget.Android,options=BuildOptions.None
   });
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("APK de prueba: "+report.summary.result);
   Debug.Log("APK de prueba generado: "+output);
   EditorUtility.RevealInFinder(output);
  }finally{
   EditorUserBuildSettings.buildAppBundle=appBundle;
   EditorUserBuildSettings.exportAsGoogleAndroidProject=exportProject;
   PlayerSettings.Android.useCustomKeystore=customKeystore;
  }
 }

 [MenuItem("Tools/DimDam/Crear AAB Android")]
 public static void BuildAndroid(){
  Validate();
  PlayerSettings.companyName="Winner Game Jam";PlayerSettings.productName=ProductName;
  PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,Package);
  if(PlayerSettings.Android.bundleVersionCode<2){
   PlayerSettings.Android.bundleVersionCode=2;
   PlayerSettings.bundleVersion="1.0.1";
  }
  PlayerSettings.Android.minSdkVersion=(AndroidSdkVersions)26;
  PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;
  PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
  PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
  EditorUserBuildSettings.buildAppBundle=true;
  var signing=LoadSigning();
  string key=signing["keystore"];
  string storePassword=signing["storePassword"];
  string alias=signing["alias"];
  string keyPassword=signing["keyPassword"];
  if(!File.Exists(key)||string.IsNullOrEmpty(storePassword)||string.IsNullOrEmpty(alias)||string.IsNullOrEmpty(keyPassword))throw new Exception("ANDROID_BUILD_NEEDS_UPLOAD_KEY: check signing.txt and keystore");
  PlayerSettings.Android.useCustomKeystore=true;PlayerSettings.Android.keystoreName=key;PlayerSettings.Android.keystorePass=storePassword;PlayerSettings.Android.keyaliasName=alias;PlayerSettings.Android.keyaliasPass=keyPassword;
  PrepareAndroidMovies();
  string output=Path.Combine(OutputRoot,DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"),"SiempreJuntos.aab");
  Directory.CreateDirectory(Path.GetDirectoryName(output));
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=GameDuoCampaignBuildScenes(),locationPathName=output,target=BuildTarget.Android,options=BuildOptions.None});
  if(report.summary.result!=BuildResult.Succeeded)throw new Exception("ANDROID_BUILD_FAILED: "+report.summary.result);
  Debug.Log("ANDROID_AAB_SUCCEEDED: "+Path.GetFullPath(output)+" / "+report.summary.totalSize+" bytes");
 }
 static void PrepareAndroidMovies(){
  var library=Resources.Load<GameDuoMovieLibrary>("GameDuoMovies");
  Require(library&&library.clips!=null&&library.clips.Length==9,"nine campaign movies bundled");
  foreach(var clip in library.clips){
   Require(clip,"cinematic reference is present");
   var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as VideoClipImporter;
   if(!importer)throw new Exception("Missing video importer: "+clip.name);
   var settings=importer.GetTargetSettings("Android")??new VideoImporterTargetSettings();
   if(settings.enableTranscoding&&settings.codec==VideoCodec.H264&&settings.resizeMode==VideoResizeMode.CustomSize&&
    settings.customWidth==1280&&settings.customHeight==720&&settings.bitrateMode==VideoBitrateMode.High)continue;
   settings.enableTranscoding=true;
   settings.codec=VideoCodec.H264;
   settings.resizeMode=VideoResizeMode.CustomSize;
   settings.customWidth=1280;
   settings.customHeight=720;
   settings.bitrateMode=VideoBitrateMode.High;
   importer.SetTargetSettings("Android",settings);
   importer.SaveAndReimport();
  }
 }
 static string[] GameDuoCampaignBuildScenes(){var scenes=new string[GameDuoCampaign.Scenes.Length+1];scenes[0]=GameDuoMainMenu.ScenePath;Array.Copy(GameDuoCampaign.Scenes,0,scenes,1,GameDuoCampaign.Scenes.Length);return scenes;}
 static Dictionary<string,string> LoadSigning(){string file=Environment.GetEnvironmentVariable("DIMDAM_SIGNING_FILE");if(string.IsNullOrEmpty(file))file=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"DimDam Play Store Handoff","signing.txt");if(!File.Exists(file))throw new Exception("ANDROID_BUILD_NEEDS_UPLOAD_KEY: "+file);var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(var line in File.ReadAllLines(file)){int split=line.IndexOf('=');if(split>0)values[line.Substring(0,split).Trim()]=line.Substring(split+1).Trim();}foreach(var name in new[]{"keystore","storePassword","alias","keyPassword"})if(!values.ContainsKey(name))throw new Exception("ANDROID_BUILD_NEEDS_UPLOAD_KEY: missing "+name);return values;}
}

public sealed class GameDuoAndroidIcon : IPreprocessBuildWithReport {
 public int callbackOrder=>0;

 const string SourcePath="Assets/GameDuo/Branding/SiempreJuntosIcon.png";
 const string AdaptivePath="Assets/GameDuo/Branding/SiempreJuntosIconAdaptive.png";
 const int TextureSize=512;
 const float ArtworkScale=.55f;

 public void OnPreprocessBuild(BuildReport report){
  if(report.summary.platform!=BuildTarget.Android)return;
  var icon=PrepareAdaptiveTexture();
  var kind=UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
  var icons=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);
  foreach(var slot in icons)slot.SetTextures(icon,icon);
  PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android,kind,icons);
 }

 static Texture2D PrepareAdaptiveTexture(){
  if(!File.Exists(SourcePath))throw new BuildFailedException("No se encontró el icono Android de Siempre Juntos.");
  var original=new Texture2D(2,2,TextureFormat.RGBA32,false);
  var adaptive=new Texture2D(TextureSize,TextureSize,TextureFormat.RGBA32,false);
  try{
   if(!ImageConversion.LoadImage(original,File.ReadAllBytes(SourcePath)))
    throw new BuildFailedException("No se pudo leer el icono Android de Siempre Juntos.");
   original.wrapMode=TextureWrapMode.Clamp;
   var pixels=new Color32[TextureSize*TextureSize];
   var navy=new Color(.035f,.055f,.17f,1);
   // Center the full illustration and extend its edge colors into the launcher mask.
   for(int y=0;y<TextureSize;y++){
    float v=((y+.5f)/TextureSize-.5f)/ArtworkScale+.5f;
    for(int x=0;x<TextureSize;x++){
     float u=((x+.5f)/TextureSize-.5f)/ArtworkScale+.5f;
     var color=original.GetPixelBilinear(Mathf.Clamp01(u),Mathf.Clamp01(v));
     float distance=Mathf.Max(Mathf.Abs(u-.5f),Mathf.Abs(v-.5f))-.5f;
     var pixel=Color.Lerp(color,navy,Mathf.SmoothStep(0,1,distance/.16f));
     pixel.a=1;
     pixels[y*TextureSize+x]=pixel;
    }
   }
   adaptive.SetPixels32(pixels);
   adaptive.Apply();
   var png=ImageConversion.EncodeToPNG(adaptive);
   if(!File.Exists(AdaptivePath)||!System.Linq.Enumerable.SequenceEqual(File.ReadAllBytes(AdaptivePath),png)){
    File.WriteAllBytes(AdaptivePath,png);
    AssetDatabase.ImportAsset(AdaptivePath,ImportAssetOptions.ForceSynchronousImport);
   }
  }finally{
   UnityEngine.Object.DestroyImmediate(adaptive);
   UnityEngine.Object.DestroyImmediate(original);
  }
  var icon=AssetDatabase.LoadAssetAtPath<Texture2D>(AdaptivePath);
  if(!icon)throw new BuildFailedException("No se pudo importar el icono adaptativo de Siempre Juntos.");
  return icon;
 }
}
public sealed class GameDuoAndroidFeatures : UnityEditor.Android.IPostGenerateGradleAndroidProject {
 public int callbackOrder=>100;

 public void OnPostGenerateGradleAndroidProject(string path){
  // Set the optional capability in the highest-priority launcher manifest.
  string manifest=Path.GetFullPath(Path.Combine(path,"..","launcher","src","main","AndroidManifest.xml"));
  if(!File.Exists(manifest))throw new BuildFailedException("No se encontró el manifiesto del lanzador Android.");
  var xml=new System.Xml.XmlDocument();
  xml.Load(manifest);
  var root=xml.DocumentElement;
  if(root==null)throw new BuildFailedException("El manifiesto del lanzador Android está vacío.");
  const string android="http://schemas.android.com/apk/res/android";
  const string landscape="android.hardware.screen.landscape";
  foreach(System.Xml.XmlNode node in root.ChildNodes){
   if(node is System.Xml.XmlElement element&&element.Name=="uses-feature"&&element.GetAttribute("name",android)==landscape){
    SetAndroidAttribute(element,"required","false",android);
    xml.Save(manifest);
    return;
   }
  }
  var feature=xml.CreateElement("uses-feature");
  SetAndroidAttribute(feature,"name",landscape,android);
  SetAndroidAttribute(feature,"required","false",android);
  root.PrependChild(feature);
  xml.Save(manifest);
 }

 static void SetAndroidAttribute(System.Xml.XmlElement element,string name,string value,string ns){
  var attribute=element.OwnerDocument.CreateAttribute("android",name,ns);
  attribute.Value=value;
  element.SetAttributeNode(attribute);
 }
}