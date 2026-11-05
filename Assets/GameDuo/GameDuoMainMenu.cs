using UnityEngine;
namespace CoopBeam {
public sealed class GameDuoMainMenu : MonoBehaviour {
 public const string ScenePath="Assets/GameDuo/Scenes/00_Menu.unity";
 Texture2D background; GameDuoTutorial controls; AudioSource music; bool multiplayer,privacy; string joinIp="192.168.1.";
 void Awake(){
  Time.timeScale=1;AudioListener.pause=false;
  background=Resources.Load<Texture2D>("MainMenu");
  controls=gameObject.AddComponent<GameDuoTutorial>();
  GameDuoLan.Ensure();
  var cameraObject=new GameObject("Menu camera");
  cameraObject.AddComponent<AudioListener>();
  var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
  music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;
  music.spatialBlend=0;music.ignoreListenerPause=true;music.volume=0;
  music.clip=Resources.Load<AudioClip>("MenuTheme");if(music.clip)music.Play();
 }
 void Update(){
  if(music)music.volume=Mathf.MoveTowards(music.volume,GameDuoCinema.Busy?0:.38f,Time.unscaledDeltaTime*.95f);
 }
 public void StartGame(){if(!GameDuoCinema.Busy)GameDuoCinema.Play(new[]{0},GameDuoCampaign.Scenes[0]);}
 void OnGUI(){
  GUI.depth=100;
  GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.blackTexture);
  float scale=Mathf.Min(Screen.width/1672f,Screen.height/941f);
  var matrix=GUI.matrix;
  GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1672*scale)/2,(Screen.height-941*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
  if(background)GUI.DrawTexture(new Rect(0,0,1672,941),background);
  if(!controls.Reviewing&&!GameDuoCinema.Busy&&!multiplayer&&!privacy){
   if(GUI.Button(new Rect(338,791,265,77),GUIContent.none,GUIStyle.none))controls.OpenControls();
   if(GUI.Button(new Rect(660,781,365,92),GUIContent.none,GUIStyle.none))StartGame();
   if(GUI.Button(new Rect(1082,791,263,77),GUIContent.none,GUIStyle.none))Quit();
   if(GUI.Button(new Rect(662,690,362,72),"MULTIJUGADOR LOCAL"))multiplayer=true;
  }
  if(!privacy&&!controls.Reviewing&&!GameDuoCinema.Busy&&GUI.Button(new Rect(35,875,240,42),"PRIVACIDAD"))privacy=true;
  GUI.matrix=matrix;
  if(privacy)DrawPrivacy();
  else if(multiplayer&&!controls.Reviewing&&!GameDuoCinema.Busy)DrawMultiplayer();
 }
 void DrawPrivacy(){
  var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/960f,Screen.height/540f,1));GUI.depth=40;
  CoopPromptUI.Panel(new Rect(135,55,690,430),CoopPromptUI.Mint);
  CoopPromptUI.Text(new Rect(175,75,610,42),"PRIVACIDAD DE DIMDAM",27,CoopPromptUI.Mint);
  CoopPromptUI.Text(new Rect(175,125,610,235),"El juego guarda el progreso de la campaña en este dispositivo. No pide cuenta, no muestra anuncios y no incluye compras ni análisis de uso.\n\nEn multijugador local, el segundo móvil envía controles al anfitrión por la misma red Wi-Fi. Se muestra la IP local para conectar los dispositivos. No usamos servidores externos y no enviamos esa IP ni los controles a los desarrolladores.",17);
  if(GUI.Button(new Rect(350,400,260,48),"CERRAR"))privacy=false;
  GUI.matrix=old;
 }
 void DrawMultiplayer(){
  var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/960f,Screen.height/540f,1));GUI.depth=50;
  CoopPromptUI.Panel(new Rect(215,45,530,440),CoopPromptUI.Mint);
  CoopPromptUI.Text(new Rect(240,60,480,42),"MULTIJUGADOR LOCAL",25,CoopPromptUI.Mint);
  CoopPromptUI.Text(new Rect(245,105,470,52),"Ambos móviles en la misma red Wi-Fi. La partida se ve en la pantalla del anfitrión; el otro móvil funciona como mando.",14);
  if(GUI.Button(new Rect(280,160,400,45),"CREAR PARTIDA / ANFITRIÓN"))GameDuoLan.StartHost();
  CoopPromptUI.Text(new Rect(260,210,440,42),GameDuoLan.Status,14);
  joinIp=GUI.TextField(new Rect(320,265,320,35),joinIp,32);
  if(GUI.Button(new Rect(280,310,400,45),"UNIRME CON ESA IP"))GameDuoLan.Join(joinIp);
  if(GameDuoLan.Mode==GameDuoLan.LanMode.Host&&GUI.Button(new Rect(280,370,400,45),"JUGAR COMO ANFITRIÓN"))StartGame();
  if(GUI.Button(new Rect(280,430,400,35),"VOLVER / JUGAR EN UNA PANTALLA")){GameDuoLan.Local();multiplayer=false;}
  GUI.matrix=old;
 }
 static void Quit(){
#if UNITY_EDITOR
  UnityEditor.EditorApplication.isPlaying=false;
#else
  Application.Quit();
#endif
 }
}
}
