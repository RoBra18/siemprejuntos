using UnityEngine;
namespace CoopBeam {
// Native GUI shapes stay crisp at any resolution; arrows do not depend on font glyphs.
public static class CoopPromptUI {
 public sealed class StartGate {
  readonly bool[] ready=new bool[2];
  public bool Confirmed(int player)=>player>=0&&player<2&&ready[player];
  public void Reset(){ready[0]=ready[1]=false;}
  public bool Poll(){if(GameDuoCinema.Busy)return false;var k=UnityEngine.InputSystem.Keyboard.current;if(k!=null){if(k.fKey.wasPressedThisFrame)ready[0]=true;if(k.enterKey.wasPressedThisFrame||k.numpadEnterKey.wasPressedThisFrame)ready[1]=true;}return ready[0]&&ready[1];}
  public void Draw(float y=175,bool showHeroes=true){if(TouchControls&&GameDuoCampaign.InCampaignScene){if(showHeroes){Hero(new Rect(225,y-62,40,55),0);Hero(new Rect(375,y-50,32,43),1);}if(Button(new Rect(190,y,260,46),"Empezar",Mint))ready[0]=ready[1]=true;return;}for(int i=0;i<2;i++){float x=100+i*230;var c=CoopPresentation.PlayerColor(i);if(showHeroes)Hero(new Rect(x+70,y-62,40,55),i);Panel(new Rect(x,y,210,46),ready[i]?Mint:c);if(GUI.Button(new Rect(x+3,y+3,204,40),(i==0?"MIMO / F":"PIPRO / ENTER")+" / EMPEZAR"))ready[i]=true;}}
 }
 public static bool TouchControls=>GameDuoMobileInput.TouchControls;
 public static bool Button(Rect rect,string label,Color accent){
  Panel(rect,accent);
  bool clicked=GUI.Button(rect,GUIContent.none,GUIStyle.none);
  Text(rect,label,15,accent);
  return clicked;
 }
 public static Matrix4x4 FitSafeArea(){
  Rect safe=Screen.safeArea;
  if(safe.width<=0||safe.height<=0)safe=new Rect(0,0,Screen.width,Screen.height);
  float scale=Mathf.Min(safe.width/640f,safe.height/360f);
  return Matrix4x4.TRS(new Vector3(safe.center.x-320*scale,Screen.height-safe.center.y-180*scale),
   Quaternion.identity,new Vector3(scale,scale,1));
 }
 public static void Arrow(Vector2 center,int direction,float size,Color color){
  Vector2 d=direction==0?Vector2.down:direction==1?Vector2.left:direction==2?Vector2.up:Vector2.right;
  Vector2 tip=center+d*size, side=Vector2.Perpendicular(d)*size*.65f;
  float width=Mathf.Max(2,size*.2f);
  Line(center-d*size,tip,width,color);Line(tip,center+side,width,color);Line(tip,center-side,width,color);
 }
 public static void Bolt(Vector2 center,float radius,Color color){
  Ring(center,radius,1,color,2);
  var old=GUI.color;GUI.color=color;
  GUI.DrawTexture(new Rect(center.x-radius*.72f,center.y-radius*.72f,radius*1.44f,radius*1.44f),GameDuoMobileInput.PowerIcon());
  GUI.color=old;
 }
 public static void Loading(){
  var center=new Vector2(320,164);
  Ring(center,16,1,new Color(.3f,.25f,.48f),2);
  var previous=center;
  for(int i=0;i<=16;i++){
   float angle=Time.unscaledTime*5+i*.19f;
   var next=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*16;
   if(i>0)Line(previous,next,2,Mint);
   previous=next;
  }
  Text(new Rect(220,189,200,32),"Cargando…",15);
 }
 public static void Stick(Vector2 center,float radius,int direction,Color color,bool active=true){
  Ring(center,radius,1,color,2);
  Color highlight=active?color:Color.Lerp(color,Color.gray,.7f);
  if(direction>=0)Arrow(center,direction,radius*.57f,highlight);
  else Ring(center,radius*.27f,1,highlight,3);
 }
 public static readonly Color Ink=new Color(.055f,.045f,.12f,.97f), Mint=new Color(.45f,1,.77f), Amber=new Color(1,.77f,.28f);
 public static void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
 public static void Text(Rect rect,string value,int size=12,Color? color=null){var style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=size,fontStyle=FontStyle.Bold,wordWrap=true};style.normal.textColor=color??Color.white;GUI.Label(rect,value,style);}
 public static void Panel(Rect rect,Color edge){Fill(rect,edge);Fill(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),Ink);}
 public static void Line(Vector2 a,Vector2 b,float width,Color color){var matrix=GUI.matrix;float angle=Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg;GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(a.x,a.y),Quaternion.Euler(0,0,angle),Vector3.one);Fill(new Rect(0,-width*.5f,Vector2.Distance(a,b),width),color);GUI.matrix=matrix;}
 public static void Ring(Vector2 center,float radius,float progress,Color color,float width=3){int count=Mathf.CeilToInt(48*Mathf.Clamp01(progress));for(int i=0;i<count;i++){float a=(-90+i*7.5f)*Mathf.Deg2Rad,b=(-90+(i+1)*7.5f)*Mathf.Deg2Rad;Line(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width,color);}}
 public static void Key(Rect r,string label,Color color,bool active=false,int direction=-1){
  Fill(new Rect(r.x,r.y+4,r.width,r.height),new Color(.015f,.01f,.04f));
  Fill(r,active?color:new Color(.23f,.22f,.34f));
  Fill(new Rect(r.x+3,r.y+3,r.width-6,r.height-7),active?Color.Lerp(color,Ink,.55f):Ink);
  if(direction<0)Text(r,label,Mathf.Clamp((int)(r.height*.32f),10,24),active?Color.white:color);
  else {
   Arrow(r.center,direction,r.height*.22f,color);
  }
 }
 public static void Pad(Vector2 center,int player,int selected,bool active,float size=27){
  Vector2[] offsets={new Vector2(0,-1),new Vector2(-1,0),new Vector2(0,1),new Vector2(1,0)};string[] letters={"W","A","S","D"};
  Color c=CoopPresentation.PlayerColor(player);
  for(int i=0;i<4;i++){Vector2 p=center+offsets[i]*(size+4);Key(new Rect(p.x-size*.5f,p.y-size*.5f,size,size),letters[i],c,active&&i==selected,player==1?i:-1);}
 }
 static PixelAnimationSet controlHeroes;
 public static Sprite HeroSprite(int player){
  if(player<0||player>1)return null;
  var game=GameManager.Instance;
  var health=game&&game.players!=null&&game.players.Length>player?game.players[player]:null;
  var controller=health?health.GetComponent<PlayerController2D>():null;
  if(controller&&controller.sprite&&controller.sprite.sprite)return controller.sprite.sprite;
  // Menus have no live players; use the same original character art directly.
  if(!controlHeroes)controlHeroes=Resources.Load<PixelAnimationSet>("GameDuoControlHeroes");
  return controlHeroes&&controlHeroes.frames!=null&&controlHeroes.frames.Length>player?controlHeroes.frames[player]:null;
 }
 public static void Hero(Rect r,int player){
  var s=HeroSprite(player);if(!s)return;var uv=s.textureRect;
  float fit=Mathf.Min(r.width/uv.width,r.height/uv.height);
  GUI.DrawTextureWithTexCoords(new Rect(r.center.x-uv.width*fit*.5f,r.yMax-uv.height*fit,uv.width*fit,uv.height*fit),s.texture,new Rect(uv.x/s.texture.width,uv.y/s.texture.height,uv.width/s.texture.width,uv.height/s.texture.height));
 }
 public static void Dance(int player,int key,int beat,int hits,float phase,bool answered,bool correct){
  bool window=phase>=CoopInteractionDirector.HitStart/CoopInteractionDirector.BeatDuration&&phase<=CoopInteractionDirector.HitEnd/CoopInteractionDirector.BeatDuration;
  Color c=answered?(correct?Mint:new Color(1,.4f,.45f)):CoopPresentation.PlayerColor(player);
  bool touch=GameDuoCampaign.InCampaignScene&&TouchControls;
  Panel(new Rect(135,75,370,220),c);
  Text(new Rect(150,83,340,24),"Danza del Enlace",17);
  for(int i=0;i<2;i++){
   float x=i==0?181:459;bool turn=i==player;
   Hero(new Rect(x-22,i==0?115:133,i==0?44:29,i==0?60:42),i);
   Text(new Rect(x-44,177,88,20),i==0?"Mimo":"Pipro",12,turn?c:Color.gray);
   if(turn)Fill(new Rect(x-15,198,30,2),c);
   if(touch)Stick(new Vector2(x,230),24,turn?key:-1,CoopPresentation.PlayerColor(i),turn&&window&&!answered);
   else Pad(new Vector2(x,231),i,key,turn&&!answered,19);
  }
  Ring(new Vector2(320,180),50,1,new Color(.2f,.2f,.3f),5);
  Ring(new Vector2(320,180),50,phase,c,5);
  float approach=Mathf.Lerp(78,48,Mathf.Clamp01(phase/.22f));if(!answered&&phase<.22f)Ring(new Vector2(320,180),approach,1,c,2);
  if(touch)Stick(new Vector2(320,180),29,key,c,!answered&&window);
  else Key(new Rect(294,154,52,52),new[]{"W","A","S","D"}[key],c,!answered&&window,player==1?key:-1);
  Text(new Rect(235,217,170,26),answered?(correct?"¡Bien!":"Fuera de ritmo"):phase<CoopInteractionDirector.HitStart/CoopInteractionDirector.BeatDuration?"Prepárate":window?"¡Ahora!":"",14,c);
  for(int i=0;i<12;i++)Fill(new Rect(230+i*15,272,11,6),i<beat?Mint:i==beat?c:new Color(.22f,.2f,.3f));
  Text(new Rect(235,283,170,14),hits+(hits==1?" acierto":" aciertos"),10);
 }
 public static void Lesson(string id,string title){
  Panel(new Rect(60,50,520,260),Mint);Text(new Rect(75,60,490,32),title,17,Mint);
  string[] captions;
  if(id=="gate")captions=new[]{"Uno en cada sello","Recojan la llave","Mantengan la tecla\nen la cerradura"};
  else if(id=="dance")captions=new[]{"Q + SHIFT DER\npara acordar","Miren el color\ny la tecla central","Pulsen al brillar\n12 pulsos en equipo"};
  else if(id=="rescue")captions=new[]{"Un principe cae","El otro se acerca","Permanezcan juntos\ndurante 1.5 segundos"};
  else if(id=="guard")captions=new[]{"Uno atrae al guardia","El otro va detras","Crucen su cuerpo\ncon el lazo"};
  else if(id=="sling")captions=new[]{"Mimo mantiene A\npara tensar","Pipro apunta\ncon las flechas","D + CTRL DER\npara soltar juntos"};
  else if(id=="stars")captions=new[]{"Busquen su color","Cada uno a su estrella","Sostengan el par\ny repitan tres veces"};
  else captions=new[]{"Recojan el poder","Junten las aureolas","Sepárense para\nactivar el lazo"};
  for(int i=0;i<3;i++){
   float x=80+i*164;Panel(new Rect(x,104,152,155),new Color(.3f,.27f,.45f));Text(new Rect(x+8,110,136,18),(i+1).ToString(),13,Mint);
   if(id=="dance"||id=="sling"){
    if(i==0)Key(new Rect(x+48,141,56,37),id=="dance"?"Q":"A",CoopPresentation.PlayerColor(0),true);
    else if(i==1)Pad(new Vector2(x+76,157),1,0,true,15);
    else Key(new Rect(x+48,141,56,37),id=="dance"?"W":"CTRL",Mint,true);
   } else {
    float separation=(id=="power"&&i==1||id=="rescue"&&i>0)?12:34;
    Hero(new Rect(x+65-separation,140,31,id=="rescue"&&i==0?16:45),0);
    Hero(new Rect(x+68+separation,155,20,30),1);
    Line(new Vector2(x+76-separation,167),new Vector2(x+77+separation,167),2,Mint);
    if(id=="gate"&&i==2){Key(new Rect(x+10,140,29,28),"S",Mint);Key(new Rect(x+114,140,29,28),"",Mint,false,2);}
   }
   Text(new Rect(x+5,201,142,50),captions[i],12);
  }
  Key(new Rect(255,274,65,23),"ESC",Mint);Text(new Rect(328,274,135,23),"CONTINUAR",12);
 }
}
}
