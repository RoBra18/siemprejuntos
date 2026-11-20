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
 const string Package="com.winnergamejam.dimdam";
 const string Output="Builds/Android/DimDam.aab";
 static void Require(bool condition,string message){if(!condition)throw new Exception("MOBILE_QA_FAILED: "+message);Debug.Log("MOBILE_QA_OK: "+message);}
 [MenuItem("Tools/DimDam/Validar móvil y multijugador")]
 public static void Validate(){
  Require(File.Exists(GameDuoMainMenu.ScenePath),"menu scene present");
  Require(GameDuoCampaign.Scenes.Length>0,"campaign scenes present");
  foreach(var path in GameDuoCampaign.Scenes){Require(File.Exists(path),"scene "+path);var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{if(path==GameDuoCampaign.Scenes[3])Require(FindInScene<IcePenguinRound>(scene),"penguin rescue round in "+scene.name);else{var game=FindInScene<GameManager>(scene);Require(game&&game.players!=null&&game.players.Length==2&&game.players[0]&&game.players[1],"two players in "+scene.name);}}finally{EditorSceneManager.CloseScene(scene,true);}}
  var menu=EditorSceneManager.OpenScene(GameDuoMainMenu.ScenePath,OpenSceneMode.Additive);
  try{Require(FindInScene<GameDuoMainMenu>(menu),"multiplayer menu component present");}finally{EditorSceneManager.CloseScene(menu,true);}
  CheckTouchMapping();
  try{CheckHostTransport();CheckGuestTransport();}
  finally{GameDuoLan.Local();if(GameDuoLan.Instance)UnityEngine.Object.DestroyImmediate(GameDuoLan.Instance.gameObject);}
  Debug.Log("MOBILE_QA_COMPLETE");
 }
 static T FindInScene<T>(Scene scene) where T:Component{foreach(var root in scene.GetRootGameObjects()){var match=root.GetComponentInChildren<T>(true);if(match)return match;}return null;}
 static void CheckTouchMapping(){
  var go=new GameObject("Mobile input QA");var input=go.AddComponent<GameDuoMobileInput>();
  var type=typeof(GameDuoMobileInput);var instance=type.GetField("instance",BindingFlags.Static|BindingFlags.NonPublic);var apply=type.GetMethod("Apply",BindingFlags.Instance|BindingFlags.NonPublic);var edges=type.GetMethod("Edges",BindingFlags.Instance|BindingFlags.NonPublic);
  instance.SetValue(null,input);
  apply.Invoke(input,new object[]{new Vector2(164,424)});apply.Invoke(input,new object[]{new Vector2(796,424)});
  Require(GameDuoMobileInput.Get(0).axis>.9f&&GameDuoMobileInput.Get(1).axis<-.9f,"both local joysticks have independent axes");
  apply.Invoke(input,new object[]{new Vector2(112,418-40)});apply.Invoke(input,new object[]{new Vector2(634,300)});
  edges.Invoke(input,new object[]{0});edges.Invoke(input,new object[]{1});
  Require(GameDuoMobileInput.Get(0).jumpPressed&&GameDuoMobileInput.Get(1).ultimatePressed,"jump and ultimate touch mapping");
  instance.SetValue(null,null);UnityEngine.Object.DestroyImmediate(go);
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
 [MenuItem("Tools/DimDam/Crear AAB Android")]
 public static void BuildAndroid(){
  Validate();
  PlayerSettings.companyName="Winner Game Jam";PlayerSettings.productName="DimDam";
  PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,Package);
  PlayerSettings.bundleVersion="1.0.0";PlayerSettings.Android.bundleVersionCode=1;
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
  Directory.CreateDirectory(Path.GetDirectoryName(Output));
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=GameDuoCampaignBuildScenes(),locationPathName=Output,target=BuildTarget.Android,options=BuildOptions.None});
  if(report.summary.result!=BuildResult.Succeeded)throw new Exception("ANDROID_BUILD_FAILED: "+report.summary.result);
  Debug.Log("ANDROID_AAB_SUCCEEDED: "+Path.GetFullPath(Output)+" / "+report.summary.totalSize+" bytes");
 }
 static string[] GameDuoCampaignBuildScenes(){var scenes=new string[GameDuoCampaign.Scenes.Length+1];scenes[0]=GameDuoMainMenu.ScenePath;Array.Copy(GameDuoCampaign.Scenes,0,scenes,1,GameDuoCampaign.Scenes.Length);return scenes;}
 static Dictionary<string,string> LoadSigning(){string file=Environment.GetEnvironmentVariable("DIMDAM_SIGNING_FILE");if(string.IsNullOrEmpty(file))file=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"DimDam Play Store Handoff","signing.txt");if(!File.Exists(file))throw new Exception("ANDROID_BUILD_NEEDS_UPLOAD_KEY: "+file);var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(var line in File.ReadAllLines(file)){int split=line.IndexOf('=');if(split>0)values[line.Substring(0,split).Trim()]=line.Substring(split+1).Trim();}foreach(var name in new[]{"keystore","storePassword","alias","keyPassword"})if(!values.ContainsKey(name))throw new Exception("ANDROID_BUILD_NEEDS_UPLOAD_KEY: missing "+name);return values;}
}
