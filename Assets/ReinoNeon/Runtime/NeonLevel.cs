using UnityEngine;
using UnityEngine.InputSystem;
namespace CoopBeam {
public class NeonLevel:MonoBehaviour {
 readonly CoopPromptUI.StartGate startGate=new CoopPromptUI.StartGate();
 public const float CoreStepDuration=25; public const string ScenePath="Assets/ReinoNeon/Scenes/ReinoNeon.unity";
 public GameManager game; public NeonDevice[] crossed; public NeonDevice entryGate,relayGate,finalGate;
 public BubblegumHeavy battery; public Transform socketA,socketB,terminal,coreA,coreB,coreC,padA,padB,fragment,exit;
 public GameObject relayBridge,coreBridge;
 [SerializeField] GameObject coreBridgeInactive;public SpriteRenderer reactor; public Sprite[] reactorFrames;
 public int Checkpoint{get;private set;} public int CoreStep{get;private set;} public bool SocketA{get;private set;}public bool Routed{get;private set;}public bool RelayDone{get;private set;} public bool Restored{get;private set;}public bool Collected{get;private set;}public bool Completed{get;private set;}
 public float Remaining=>Mathf.Max(0,deadline-Time.time); public int CrossedCount{get{int n=0;foreach(var d in crossed)if(d.powered)n++;return n;}}
 public bool TimedOut{get;private set;}
 public float RoundRemaining{get;private set;}=60; bool roundStarted; float deadline,charge,rewardTime;string notice="Restablece la energia del nucleo trabajando en equipo";float noticeUntil=12;readonly float[] last={-1,-1};
 void Start(){game.BeginExploration();relayBridge.SetActive(false);AddCoreBridgeLights();SetCoreBridge(false);battery.maxX=18;}
 void AddCoreBridgeLights(){
  if(!GameDuoCampaign.InCampaignScene||!coreBridge.TryGetComponent<BoxCollider2D>(out var surface))return;
  var top=surface.offset+Vector2.up*(surface.size.y*.5f);
  AddBridgeLight("Borde cian del puente",top,new Vector2(surface.size.x,.14f),new Color(.05f,.9f,1f));
  AddBridgeLight("Línea luminosa del puente",top+Vector2.up*.025f,new Vector2(surface.size.x,.048f),new Color(.82f,1f,1f));
 }
 void AddBridgeLight(string label,Vector2 point,Vector2 size,Color color){
  var light=new GameObject(label);
  light.transform.SetParent(coreBridge.transform,false);
  light.transform.localPosition=point;
  var renderer=light.AddComponent<SpriteRenderer>();
  renderer.sprite=ExpansionSprites.Square;
  var artSize=renderer.sprite.bounds.size;
  light.transform.localScale=new Vector3(size.x/artSize.x,size.y/artSize.y,1);
  renderer.color=color;
  renderer.sortingOrder=16;
 }
 void SetCoreBridge(bool active){
  coreBridge.SetActive(active);
  if(coreBridgeInactive)coreBridgeInactive.SetActive(!active);
 }
 public bool Near(int p,Transform t,float r=1.35f)=>p>=0&&p<2&&t&&!game.players[p].Dead&&Vector2.Distance(game.players[p].transform.position,t.position)<r;
 bool Reach(int p,Transform t,float r=1.35f)=>Near(p,t,r)&&!Physics2D.Linecast(game.players[p].transform.position,t.position,1<<8).collider;
 void Say(string s){notice=s;noticeUntil=Time.time+6;}
 public bool Interact(int p){
 if(!roundStarted||p<0||p>1||!game.Running||Time.time-last[p]<.2f)return false;
 foreach(var d in crossed)if(d.player==p&&!d.powered&&Reach(p,d.transform)){d.Set(true);d.target.Set(true);last[p]=Time.time;Say(p==0?"Mimo abre el camino de Pipro":"Pipro abre el camino de Mimo");return true;}
 bool ok=false;
 if(p==1&&SocketA&&Reach(p,terminal)){Routed=!Routed;Say(Routed?"Mimo, lleva la batería al segundo zócalo":"Circuito A activo");ok=true;}
 else if(RelayDone&&!Restored){
 if(p==0&&CoreStep==0&&Reach(p,coreA)){CoreStep=1;deadline=Time.time+CoreStepDuration;SetCoreBridge(true);Say("Pipro, cruza y cambia la dirección");ok=true;}
 else if(p==1&&CoreStep==1&&Reach(p,coreB)){CoreStep=2;deadline=Time.time+CoreStepDuration;Say("Mimo, activa el panel derecho");ok=true;}
 else if(p==0&&CoreStep==2&&Reach(p,coreC)){CoreStep=3;deadline=Time.time+CoreStepDuration;Say("Cada uno a su placa de carga");ok=true;}
 }
 if(ok){last[p]=Time.time;BeamFX.Burst(game.players[p].transform.position,Color.cyan,10);game.Tone(660,.1f);}return ok;
 }
 void Update(){if(GameDuoCampaign.Paused||GameDuoCinema.Busy||Completed&&GameDuoCampaign.InCampaignScene)return;
 if(TimedOut){var keyboard=Keyboard.current;if(keyboard!=null&&keyboard.rKey.wasPressedThisFrame)game.Restart();return;}
 if(!roundStarted&&startGate.Poll())roundStarted=true;
 var k=Keyboard.current;if(k!=null){if(k.rKey.wasPressedThisFrame){ResetTogether();return;}if(k.mKey.wasPressedThisFrame){CandyCampaign.Map();return;}if(k.sKey.wasPressedThisFrame)Interact(0);if(k.downArrowKey.wasPressedThisFrame)Interact(1);}
 if(!roundStarted)return;
 for(int i=0;i<2;i++)if(GameDuoMobileInput.Get(i).downPressed)Interact(i);
 if(!Completed){RoundRemaining=Mathf.Max(0,RoundRemaining-Time.deltaTime); if(RoundRemaining<=0){FailTimeout();return;}}
 foreach(var p in game.players)if(p.Dead||p.transform.position.y< -3){ResetTogether();return;}
 if(CrossedCount==6&&!entryGate.powered){entryGate.Set(true);Say("Reúnanse en el relevo");}
 if(Checkpoint<1&&entryGate.powered&&BothBeyond(13)){Checkpoint=1;Say("Mimo empuja. Pipro usa el elevador");}
 SocketA=Mathf.Abs(battery.transform.position.x-socketA.position.x)<.32f;
 relayBridge.SetActive(SocketA||RelayDone);battery.maxX=Routed?22:18;
 if(!RelayDone&&Routed&&Mathf.Abs(battery.transform.position.x-socketB.position.x)<.32f){RelayDone=true;battery.Locked=true;relayGate.Set(true);Say("Reúnanse en el núcleo");}
 if(Checkpoint<2&&RelayDone&&BothBeyond(35)){Checkpoint=2;Say("Mimo inicia. Pipro invierte. Mimo confirma");}
 if(!Restored&&CoreStep>0){if(Remaining<=0){FailTimeout();return;}else if(CoreStep==3){charge=Near(0,padA,1)&&Near(1,padB,1)?Mathf.Min(2.5f,charge+Time.deltaTime):Mathf.Max(0,charge-Time.deltaTime*.5f);if(charge>=2.5f){Restored=true;Checkpoint=3;finalGate.Set(true);rewardTime=Time.time;Say("Núcleo restaurado");game.Tone(990,.3f);}}}
 reactor.sprite=reactorFrames[Restored?Mathf.Min(3,1+(int)((Time.time-rewardTime)*1.5f)):CoreStep>0?1:0];
 if(Restored&&Time.time-rewardTime<3&&Time.frameCount%8==0)BeamFX.Burst(reactor.transform.position,Color.cyan,3);
 if(Restored&&!Collected&&Near(0,fragment,1.8f)&&Near(1,fragment,1.8f)){Collected=true;fragment.gameObject.SetActive(false);PlayerPrefs.SetInt("ReinoNeon.FriendshipFragment",1);PlayerPrefs.Save();BeamFX.Burst(fragment.position,Color.magenta,30);Say("Fragmento del corazón recuperado");}
 if(Collected&&!Completed&&Near(0,exit,1.5f)&&Near(1,exit,1.5f)){Completed=true;Say("Circuitos cruzados completados");game.Tone(1320,.4f);}
 }
 bool BothBeyond(float x)=>game.players[0].transform.position.x>x&&game.players[1].transform.position.x>x;
 void FailTimeout(){TimedOut=true;game.FailRound();foreach(var player in game.players){var body=player.GetComponent<Rigidbody2D>();if(body)body.linearVelocity=Vector2.zero;}}
 void PlaceTogether(Vector2 pos){for(int i=0;i<2;i++)game.players[i].ReviveAt(pos+Vector2.right*i*(Checkpoint==0?-.9f:1.5f));game.BeginExploration();Physics2D.SyncTransforms();}
 public void ResetTogether(){
 if(TimedOut)return;
 RoundRemaining=60;
 if(Checkpoint==0){foreach(var d in crossed){d.Set(false);d.target.Set(false);}entryGate.Set(false);}
 if(Checkpoint<2){battery.Restore();Routed=RelayDone=false;SocketA=false;relayGate.Set(false);battery.maxX=18;}
 if(!Restored){CoreStep=0;charge=0;SetCoreBridge(false);}else SetCoreBridge(true);
 PlaceTogether(Checkpoint==0?new Vector2(-8,1):Checkpoint==1?new Vector2(14.5f,1):Checkpoint==2?new Vector2(37,1):new Vector2(54,1));last[0]=last[1]=-1;Say("Equipo reunido en el último control");
 }
 void DrawInteractionPrompt(int player,Transform target){
  var camera=Camera.main;if(!camera||!Reach(player,target))return;
  var at=PixelFont.WorldToHudViewport(camera,target.position+Vector3.up*1.2f);
  if(at.z<=0||at.x<0||at.x>1||at.y<0||at.y>1)return;
  float x=Mathf.Clamp(at.x*640,52,588),y=Mathf.Clamp((1-at.y)*360,105,265);
  var color=CoopPresentation.PlayerColor(player);
  if(GameDuoCampaign.InCampaignScene&&CoopPromptUI.TouchControls)CoopPromptUI.Stick(new Vector2(x,y),16,2,color);
  else CoopPromptUI.Key(new Rect(x-16,y-16,32,29),"S",color,true,player==1?2:-1);
  CoopPromptUI.Text(new Rect(x-50,y+16,100,18),player==0?"Mimo":"Pipro",10,color);
 }
 void DrawInteractionPrompts(){
  if(!game.Running||Completed)return;
  foreach(var device in crossed)if(!device.powered)DrawInteractionPrompt(device.player,device.transform);
  if(SocketA)DrawInteractionPrompt(1,terminal);
  if(RelayDone&&!Restored){
   if(CoreStep==0)DrawInteractionPrompt(0,coreA);
   else if(CoreStep==1)DrawInteractionPrompt(1,coreB);
   else if(CoreStep==2)DrawInteractionPrompt(0,coreC);
  }
 }
 void OnGUI(){if(GameDuoCinema.Busy||Completed&&GameDuoCampaign.InCampaignScene)return;bool touch=GameDuoCampaign.InCampaignScene&&CoopPromptUI.TouchControls;GUI.depth=-10;PixelFont.Begin();PixelFont.Panel(new Rect(2,2,GameDuoCampaign.InCampaignScene?590:636,38),PixelFont.Ink,Color.cyan);PixelFont.Text(12,8,"Reino Neón · Circuitos cruzados",1,PixelFont.Cream);string objective=Completed?"Juntos brillamos":Collected?"Reúnanse en el portal":Restored?"Recojan juntos el fragmento":RelayDone?"Restauren el núcleo":entryGate.powered?(Routed?"Lleven la batería al zócalo B":SocketA?"Pipro, cambia la ruta":"Lleven la batería al zócalo A"):"Circuitos activados: "+CrossedCount+"/6";PixelFont.Text(12,25,objective,1,Color.cyan);
 if(TimedOut){PixelFont.Panel(new Rect(65,90,510,205),PixelFont.Ink,PixelFont.Pink);CoopPromptUI.Text(new Rect(105,129,430,54),"Se acabó el tiempo",24,PixelFont.Pink);if(GameDuoCampaign.InCampaignScene)GameDuoCampaign.DrawDefeatActions(240,game.Restart);else if(PixelFont.Button(new Rect(220,245,200,28),"R / REINICIAR",true))game.Restart();return;}
 PixelFont.Text(320,76,"TIEMPO "+Mathf.CeilToInt(RoundRemaining)+" S",2,RoundRemaining<30?Color.magenta:Color.cyan,true);if(!roundStarted){CoopPromptUI.Panel(new Rect(65,74,510,235),CoopPromptUI.Mint);
  if(touch){
   CoopPromptUI.Text(new Rect(105,93,430,37),"Circuitos cruzados",20,CoopPromptUI.Mint);
   CoopPromptUI.Stick(new Vector2(320,166),25,2,CoopPromptUI.Mint);
   CoopPromptUI.Text(new Rect(120,262,400,28),"Inclinen el joystick hacia abajo junto al interruptor.",13);
  }
  startGate.Draw(210,!touch);return;}
 DrawInteractionPrompts();
 if(!touch){
  PixelFont.Panel(new Rect(2,328,636,30),PixelFont.Ink,Color.magenta);
  PixelFont.Text(320,334,"Activar: Mimo con S · Pipro con flecha abajo",1,PixelFont.Cream,true);
  if(!GameDuoCampaign.InCampaignScene)PixelFont.Text(10,347,"R: reagrupar · M: mapa · Control "+Checkpoint+"/3",1,Color.cyan);
 }
 if(Time.time<noticeUntil){CoopPromptUI.Panel(new Rect(140,302,360,24),Color.cyan);CoopPromptUI.Text(new Rect(147,302,346,24),notice,11,PixelFont.Cream);}
 if(CoreStep>0&&!Restored){PixelFont.Text(320,49,"ESTABILIDAD "+Mathf.CeilToInt(Remaining)+" / CARGA "+Mathf.RoundToInt(charge/2.5f*100)+"%",1,Color.cyan,true);PixelFont.Fill(new Rect(220,62,200*charge/2.5f,4),Color.cyan);}
 if(Completed&&!GameDuoCampaign.InCampaignScene&&PixelFont.Button(new Rect(180,80,280,28),"VOLVER AL MAPA",true))CandyCampaign.Map();
 }
}
}
