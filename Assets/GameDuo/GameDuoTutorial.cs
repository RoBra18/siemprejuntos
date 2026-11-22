using System.Collections;
using UnityEngine;
namespace CoopBeam {
// Instruction pages belong only to the first campaign arena.
public sealed class GameDuoTutorial:MonoBehaviour {
 readonly CoopPromptUI.StartGate gate=new CoopPromptUI.StartGate();
 Sprite[] waspFrames;
 static GameDuoTutorial currentGuide;
 public static bool BlocksGameplay=>currentGuide&&currentGuide.AwaitingConfirmation;
 bool nextPage, skip;
 bool review; public bool Reviewing => review; public bool AwaitingConfirmation => active&&!review;
 public void OpenControls(){review=true;active=true;Step=0;}
 bool active;float oldScale;public int Step{get;private set;}
 public bool Confirmed(int player)=>gate.Confirmed(player);
 public static IEnumerator Practice(GameManager game){
  var guide=game.GetComponent<GameDuoTutorial>();if(!guide)guide=game.gameObject.AddComponent<GameDuoTutorial>();
  currentGuide=guide;
  guide.active=true;guide.oldScale=GameDuoCinema.Busy?1:Time.timeScale;Time.timeScale=0;
  for(guide.Step=0;guide.Step<4&&game.Running&&!guide.skip;guide.Step++){
   guide.gate.Reset();guide.nextPage=false;
   while(game.Running&&!guide.skip&&(GameDuoCinema.Busy||!guide.nextPage&&!guide.gate.Poll()))yield return null;
   // Do not reuse the key press that confirmed the previous page.
   yield return null;
  }
  guide.active=false;currentGuide=null;Time.timeScale=guide.oldScale;Destroy(guide);
 }
 void OnGUI(){if(!active||GameDuoCinema.Busy)return;GUI.depth=-60;var matrix=GUI.matrix;var color=GUI.color;
  CoopPromptUI.Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.015f,.01f,.045f,.82f));
  GUI.matrix=CoopPromptUI.FitSafeArea();
  bool touch=CoopPromptUI.TouchControls;
  CoopPromptUI.Panel(new Rect(40,18,560,330),CoopPromptUI.Mint);
  string[] titles={"Movimiento y combate","Rescate","Poder compartido","Danza del Enlace"};
  string[] instructions={
   "Mimo: A/D para moverse, W para saltar y S para bajar. Pipro: flechas.\nRodeen al enemigo y atraviésenlo con el lazo.",
   "Si tu compañero cae, quédate a su lado durante 1,5 s para revivirlo.\nSi ambos caen, termina la partida.",
   "Junten sus auras durante 0,6 s y sepárense para activar el poder." ,
   "Con la barra llena, Mimo pulsa Q y Pipro, Shift derecho.\nSigan el ritmo por turnos con WASD o las flechas."
  };
  if(touch){
   instructions[0]="Usen el joystick: a los lados para moverse, arriba para saltar y abajo para bajar.\nRodeen al enemigo y atraviésenlo con el lazo.";
   instructions[3]="Con la barra llena, cada jugador toca su rayo.\nEn tu turno, sigue la flecha al iluminarse y vuelve al centro.";
  }
  int page=Mathf.Clamp(Step,0,3);
  CoopPromptUI.Text(new Rect(60,26,520,28),titles[page],18,CoopPromptUI.Mint);
  CoopPromptUI.Text(new Rect(58,61,524,65),instructions[page],12);
  Demonstrate(page,touch);
  if(review){
   if(CoopPromptUI.Button(new Rect(65,281,145,44),"Anterior",CoopPromptUI.Mint))Step=(Step+3)%4;
   if(CoopPromptUI.Button(new Rect(247,281,146,44),"Volver al menú",CoopPromptUI.Mint)){review=false;active=false;}
   if(CoopPromptUI.Button(new Rect(430,281,145,44),"Siguiente",CoopPromptUI.Mint))Step=(Step+1)%4;
  }else if(touch){
   if(CoopPromptUI.Button(new Rect(65,279,170,46),"Omitir guía",CoopPromptUI.Amber))skip=true;
   if(CoopPromptUI.Button(new Rect(367,279,208,46),page==3?"Jugar":"Siguiente",CoopPromptUI.Mint))nextPage=true;
  }else{
   gate.Draw(277,false);
   if(CoopPromptUI.Button(new Rect(479,26,105,33),"Omitir guía",CoopPromptUI.Amber))skip=true;
  }
  CoopPromptUI.Text(new Rect(90,332,460,14),(page+1)+" / 4",10);
  GUI.color=color;GUI.matrix=matrix;
 }
 void Hero(float x,float floor,int player,bool down=false){
  var c=GUI.color;GUI.color=down?new Color(.75f,.65f,.75f):Color.white;
  CoopPromptUI.Hero(new Rect(x-18,floor-(down?9:player==0?46:32),down?42:36,down?9:player==0?46:32),player);GUI.color=c;
 }
 void Wasp(Rect r,float alpha=1){
  if(waspFrames==null)waspFrames=GameDuoCandyKing.LoadFrames("Wasp",1.25f,false);
  if(waspFrames==null)return;var sprite=waspFrames[(int)(Time.unscaledTime*10)%4];var uv=sprite.textureRect;
  var c=GUI.color;GUI.color=new Color(1,1,1,alpha);GUI.DrawTextureWithTexCoords(r,sprite.texture,new Rect(uv.x/sprite.texture.width,uv.y/sprite.texture.height,uv.width/sprite.texture.width,uv.height/sprite.texture.height));GUI.color=c;
 }
 void Aura(float x,float y,int player,float clock){CoopPresentation.DrawAura(x,y,48,CoopPresentation.PlayerColor(player),clock);}
 void Demonstrate(int page,bool touch){
  float clock=Time.unscaledTime,cycle=Mathf.Repeat(clock/2.8f,1),bounce=Mathf.Sin(cycle*Mathf.PI);
  string[][] captions={
   new[]{"Muévanse","Ataquen con el lazo","Esquiven el ataque"},
   new[]{"Compañero caído","A su lado · 1,5 s","De vuelta al combate"},
   new[]{"Recojan un aura","Juntos · 0,6 s","Sepárense"},
   new[]{"Activen la Danza","Sigan el ritmo","Ataque final"}
  };
  if(touch){
   captions[0][0]="Mover · saltar · bajar";
   captions[3][0]="Toquen sus rayos";
   captions[3][1]="Sigan la flecha";
  }
  for(int i=0;i<3;i++){
   float x=58+i*177,center=x+85,floor=228;
   CoopPromptUI.Panel(new Rect(x,133,170,135),new Color(.3f,.27f,.45f));
   CoopPromptUI.Fill(new Rect(x+9,230,152,2),new Color(.35f,.32f,.48f));
   if(page==0){
    if(i==0&&touch){int direction=(int)(cycle*4);CoopPromptUI.Stick(new Vector2(center-37,194),24,direction,CoopPresentation.PlayerColor(0));Hero(center+30+Mathf.Sin(cycle*6.28f)*12,floor-(direction==0?bounce*28:0)+(direction==2?5:0),0);}
    else if(i==0){Hero(center-24+bounce*16,floor-bounce*22,0);Hero(center+25-bounce*9,floor-bounce*14,1);CoopPromptUI.Key(new Rect(x+10,158,24,22),"W",CoopPresentation.PlayerColor(0),true);CoopPromptUI.Key(new Rect(x+133,158,24,22),"",CoopPresentation.PlayerColor(1),true,0);}
    else if(i==1){Hero(center-52,floor,0);Hero(center+52,floor,1);Wasp(new Rect(center-17,185,34,30));CoopPromptUI.Line(new Vector2(center-48,204),new Vector2(center+48,210),3,CoopPromptUI.Mint);}
    else{CoopPromptUI.Fill(new Rect(center-20,216,45,9),new Color(1,.7f,.2f,.6f));Hero(center-48+bounce*50,floor-bounce*34,0);Wasp(new Rect(center+28,166,33,27));}
   }else if(page==1){
    if(i==0){Hero(center,floor,0,true);CoopPromptUI.Text(new Rect(x+12,165,146,27),"0 corazones",12,new Color(1,.4f,.45f));}
    else if(i==1){Hero(center-12,floor,0,true);Hero(center+45-cycle*35,floor,1);CoopPromptUI.Ring(new Vector2(center-8,212),25,cycle,CoopPromptUI.Mint,3);}
    else{Hero(center-30,floor,0);Hero(center+31,floor,1);CoopPromptUI.Line(new Vector2(center-29,207),new Vector2(center+29,213),3,CoopPromptUI.Mint);}
   }else if(page==2){
    float separation=i==0?40:i==1?14:30+cycle*23;
    Aura(center-separation,205,0,clock);if(i>0)Aura(center+separation,207,1,clock);
    Hero(center-separation,floor,0);Hero(center+separation,floor,1);
    if(i==1)CoopPromptUI.Ring(new Vector2(center,207),37,cycle,CoopPromptUI.Mint,3);
    if(i==2)CoopPromptUI.Line(new Vector2(center-separation,205),new Vector2(center+separation,212),5,CoopPromptUI.Mint);
   }else{
    Aura(center-30,205,0,clock);Aura(center+30,207,1,clock);Hero(center-30,floor-bounce*5,0);Hero(center+30,floor-bounce*5,1);
    if(i==0&&touch){CoopPromptUI.Fill(new Rect(x+17,159,136,7),Color.cyan);CoopPromptUI.Bolt(new Vector2(x+24,193),13,CoopPresentation.PlayerColor(0));CoopPromptUI.Bolt(new Vector2(x+146,193),13,CoopPresentation.PlayerColor(1));}
    else if(i==1&&touch){int turn=cycle<.5f?0:1;CoopPromptUI.Stick(new Vector2(center,183),18,turn==0?0:3,CoopPresentation.PlayerColor(turn));}
    else if(i==0){CoopPromptUI.Fill(new Rect(x+17,159,136,7),Color.cyan);CoopPromptUI.Key(new Rect(x+9,180,24,23),"Q",CoopPresentation.PlayerColor(0),true);CoopPromptUI.Key(new Rect(x+129,180,34,23),"SHIFT",CoopPresentation.PlayerColor(1),true);}
    else if(i==1){bool turn=cycle<.5f;CoopPromptUI.Key(new Rect(x+29,158,26,24),"W",CoopPresentation.PlayerColor(0),turn);CoopPromptUI.Key(new Rect(x+115,158,26,24),"",CoopPresentation.PlayerColor(1),!turn,0);}
    else{Wasp(new Rect(x+7,159,30,26),1-cycle);Wasp(new Rect(x+131,159,30,26),1-cycle);CoopPromptUI.Ring(new Vector2(center,203),cycle*73,1,new Color(1,1,1,1-cycle),5);}
   }
   CoopPromptUI.Text(new Rect(x+3,242,164,23),captions[page][i],10,CoopPromptUI.Mint);
  }
 }
 void OnDestroy(){if(currentGuide==this)currentGuide=null;if(active&&!review)Time.timeScale=oldScale;if(waspFrames!=null)foreach(var frame in waspFrames)if(frame)Destroy(frame);}
}
}
