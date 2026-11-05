using UnityEngine;
using UnityEngine.InputSystem;

namespace CoopBeam {
// Runtime-created touch controls work in every campaign scene, including scenes loaded later.
public sealed class GameDuoMobileInput : MonoBehaviour {
 public struct State { public float axis; public bool jump,down,ultimate,action,dash; public bool jumpPressed,downPressed,dashPressed,ultimatePressed,actionPressed; }
 static GameDuoMobileInput instance;static Texture2D disc;
 readonly State[] current=new State[2],previous=new State[2];
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics(){instance=null;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(instance)return;var go=new GameObject("GameDuo / Controles táctiles");instance=go.AddComponent<GameDuoMobileInput>();DontDestroyOnLoad(go);}
 void Awake(){if(Application.isMobilePlatform)Screen.orientation=ScreenOrientation.LandscapeLeft;}
 public static State Get(int player){if(GameDuoLan.Mode==GameDuoLan.LanMode.Host&&player==1)return instance?instance.current[1]:default;return instance?instance.current[Mathf.Clamp(player,0,1)]:default;}
 static bool Playable=>!GameDuoCinema.Busy&&(GameManager.Instance&&GameManager.Instance.players!=null&&GameManager.Instance.players.Length==2||GameDuoCampaign.Active&&GameDuoCampaign.Instance.Stage==3);
 void Update(){
  if(!Playable&&GameDuoLan.Mode!=GameDuoLan.LanMode.Guest){current[0]=current[1]=default;return;}
  for(int i=0;i<2;i++){previous[i]=current[i];current[i]=default;}
  if(GameDuoLan.Mode==GameDuoLan.LanMode.Host){current[1]=GameDuoLan.Remote;Edges(1);}
  int count=Touchscreen.current==null?0:Touchscreen.current.touches.Count;
  for(int t=0;t<count;t++){var touch=Touchscreen.current.touches[t];if(!touch.press.isPressed)continue;Apply(ToGui(touch.position.ReadValue()));}
  var mouse=Mouse.current;if(mouse!=null&&mouse.leftButton.isPressed)Apply(ToGui(mouse.position.ReadValue()));
  Edges(0);if(GameDuoLan.Mode!=GameDuoLan.LanMode.Host)Edges(1);
  if(GameDuoLan.Mode==GameDuoLan.LanMode.Guest)GameDuoLan.Send(current[1]);
 }
 static Vector2 ToGui(Vector2 screen)=>new Vector2(screen.x*960/Screen.width,(Screen.height-screen.y)*540/Screen.height);
 void Apply(Vector2 p){
  int player=GameDuoLan.Mode==GameDuoLan.LanMode.Guest?1:p.x<480?0:1;
  if(GameDuoLan.Mode==GameDuoLan.LanMode.Host&&player==1)return;
  float stickX=player==0?112:848;
  if(GameDuoLan.Mode==GameDuoLan.LanMode.Guest)stickX=240;
  Vector2 center=new Vector2(stickX,424);
  if(Vector2.Distance(p,center)<105){current[player].axis=Mathf.Clamp((p.x-center.x)/52,-1,1);current[player].down=p.y>center.y+36;current[player].jump=p.y<center.y-38;return;}
  float actionX=GameDuoLan.Mode==GameDuoLan.LanMode.Guest?730:player==0?326:634;
  float dashX=GameDuoLan.Mode==GameDuoLan.LanMode.Guest?480:player==0?220:740;
  if(Vector2.Distance(p,new Vector2(actionX,425))<55){current[player].jump=true;return;}
  if(Vector2.Distance(p,new Vector2(actionX,300))<46){current[player].ultimate=true;return;}
  if(Vector2.Distance(p,new Vector2(dashX,300))<40){current[player].dash=true;return;}
  if(Vector2.Distance(p,new Vector2(stickX,303))<38){current[player].action=true;current[player].down=true;return;}
 }
 void Edges(int i){var s=current[i];var p=previous[i];s.jumpPressed=s.jump&&!p.jump;s.downPressed=s.down&&!p.down;s.ultimatePressed=s.ultimate&&!p.ultimate;s.actionPressed=s.action&&!p.action;s.dashPressed=s.dash&&!p.dash;current[i]=s;}
 void OnGUI(){if(!Playable&&! (GameDuoLan.Mode==GameDuoLan.LanMode.Guest))return;if(Touchscreen.current==null&&GameDuoLan.Mode==GameDuoLan.LanMode.Local&&!Application.isEditor)return;
  var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/960f,Screen.height/540f,1));GUI.depth=-80;
  int from=GameDuoLan.Mode==GameDuoLan.LanMode.Guest?1:0,to=GameDuoLan.Mode==GameDuoLan.LanMode.Local?2:GameDuoLan.Mode==GameDuoLan.LanMode.Host?1:2;
  for(int i=from;i<to;i++){float x=GameDuoLan.Mode==GameDuoLan.LanMode.Guest?240:i==0?112:848,a=GameDuoLan.Mode==GameDuoLan.LanMode.Guest?730:i==0?326:634,d=GameDuoLan.Mode==GameDuoLan.LanMode.Guest?480:i==0?220:740;var color=i==0?new Color(1,.45f,.65f,.75f):new Color(.5f,1,.7f,.75f);Circle(new Vector2(x,424),77,new Color(.08f,.07f,.15f,.5f));Circle(new Vector2(x+current[i].axis*45,424),31,color);Button(new Vector2(a,425),55,"SALTO",color,current[i].jump);Button(new Vector2(a,300),46,"ULTI",color,current[i].ultimate);Button(new Vector2(d,300),40,"DASH",color,current[i].dash);Button(new Vector2(x,303),39,"BAJAR",color,current[i].down);}
  if(GameDuoLan.Mode==GameDuoLan.LanMode.Host)CoopPromptUI.Text(new Rect(15,15,370,28),GameDuoLan.Status,13,CoopPromptUI.Mint);
  if(GameDuoLan.Mode==GameDuoLan.LanMode.Guest)CoopPromptUI.Text(new Rect(310,16,340,30),GameDuoLan.Status,16,CoopPromptUI.Mint);
  GUI.matrix=old;
 }
 static Texture2D Disc(){if(disc)return disc;disc=new Texture2D(64,64,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};for(int y=0;y<64;y++)for(int x=0;x<64;x++){float distance=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));disc.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01((31.5f-distance)*.75f)));}disc.Apply();return disc;}
 static void Circle(Vector2 c,float r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(c.x-r,c.y-r,r*2,r*2),Disc());GUI.color=old;}
 static void Button(Vector2 c,float r,string label,Color color,bool pressed){Circle(c,r,pressed?Color.white:color);CoopPromptUI.Text(new Rect(c.x-r,c.y-r,r*2,r*2),label,16,Color.black);}
}
}
