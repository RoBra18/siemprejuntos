using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoopBeam {
// Only GameDuo scene copies participate; the original standalone worlds keep their exits.
public sealed class GameDuoCampaign : MonoBehaviour {
 public static readonly string[] Scenes = {
  "Assets/GameDuo/Scenes/01_Tutorial_Chicle.unity",
  "Assets/GameDuo/Scenes/02_ReinoNeon.unity",
  "Assets/GameDuo/Scenes/03_ReyHielo.unity",
  "Assets/GameDuo/Scenes/04_RescatePinguinos.unity",
  "Assets/GameDuo/Scenes/05_JefeChicle.unity"
 };
 public static readonly string[] Titles = { "La ceremonia interrumpida", "Los desafíos del Reino Neón", "El enfrentamiento contra el Rey Hielo", "Los pingüinos del Rey Hielo", "La batalla final" };
 public static GameDuoCampaign Instance { get; private set; }
 public static bool Active => Instance != null;
 public static bool InCampaignScene => SceneManager.GetActiveScene().path.StartsWith("Assets/GameDuo/Scenes/",System.StringComparison.Ordinal);
 public int Stage { get; private set; }
 public bool Completed => finishing&&Stage==Scenes.Length-1;
 public static bool MissionWon => Active&&Instance.finishing;
 public bool ResultsReady => finishing&&Time.time>=finishAt;
 GameManager game; NeonLevel neon; BubblegumLevel mechanisms; IcePenguinRound penguins;
 bool menu, finishing; float previousScale=1, finishAt;
 const string SaveKey="GameDuo.Unlocked";
 public static int UnlockedStage => Mathf.Clamp(PlayerPrefs.GetInt(SaveKey,0),0,Scenes.Length-1);

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
 static void Install() { SceneManager.sceneLoaded-=OnSceneLoaded; SceneManager.sceneLoaded+=OnSceneLoaded; }
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void InitialScene(){OnSceneLoaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
 static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
  int stage=System.Array.IndexOf(Scenes,scene.path);
  if(stage<0)return;
  if(Instance&&Instance.gameObject.scene==scene)return;
  var host=new GameObject("GameDuo / Progresion narrativa");
  var campaign=host.AddComponent<GameDuoCampaign>();
  campaign.Stage=stage;
  campaign.Configure();
 }
 void Awake(){Instance=this;}
 void Configure(){
  CandyCampaign.ArrivalPending=false;
  game=FindAnyObjectByType<GameManager>();neon=FindAnyObjectByType<NeonLevel>();
  mechanisms=FindAnyObjectByType<BubblegumLevel>();penguins=FindAnyObjectByType<IcePenguinRound>();
  foreach(var exit in FindObjectsByType<CandyLevelExit>())exit.enabled=false;
  if(game&&game.level){
   // Runtime copy: title changes must never alter the original level asset.
   game.level=Instantiate(game.level);game.level.title=Titles[Stage];
   if(Stage==4){game.level.lesson="Rompan las defensas de caramelo y enfrenten al rey.\nLos destellos avisan los ataques. Las torretas fijan la punteria antes de disparar.\nDurante el combate, destruyan los refuerzos y crucen el nucleo cuando brille.";
    game.level.waves=new[]{
     new CandyWave{enemyTypes=new[]{0,4,5},spawnPositions=new[]{new Vector2(-7,-3.5f),new Vector2(3,.5f),new Vector2(8,-3.5f)}},
     new CandyWave{enemyTypes=new[]{1,2,5},spawnPositions=new[]{new Vector2(-4,1),new Vector2(5,-3.5f),new Vector2(-8,-3.5f)}},
     new CandyWave{enemyTypes=new[]{6,0,1,4},spawnPositions=new[]{new Vector2(0,-2.9f),new Vector2(-8,-3.4f),new Vector2(6,2),new Vector2(-4,3)}}};
   }
   if(Stage==2){var wave=game.level.waves[2];wave.enemyTypes=new[]{6,0,2,4};wave.spawnPositions=new[]{new Vector2(0,-2.65f),new Vector2(-7,-3.5f),new Vector2(7,-3.5f),new Vector2(-5,2.5f)};}
   if(Stage==0){game.level.waves=new[]{
    new CandyWave{enemyTypes=new[]{0,0,2,1,4,4},spawnPositions=new[]{new Vector2(-8,-3.5f),new Vector2(8,-3.5f),new Vector2(-5,-3.5f),new Vector2(5,-3.5f),new Vector2(-6,2.4f),new Vector2(6,2.4f)}},
    new CandyWave{enemyTypes=new[]{0,2,2,1,1,0,4,4},spawnPositions=new[]{new Vector2(-8,-3.5f),new Vector2(8,-3.5f),new Vector2(-5,-3.5f),new Vector2(5,-3.5f),new Vector2(-2,-3.5f),new Vector2(2,-3.5f),new Vector2(-6,2.8f),new Vector2(6,2.8f)}},
    new CandyWave{enemyTypes=new[]{0,2,2,1,1,0,2,4,4,4},spawnPositions=new[]{new Vector2(-9,-3.5f),new Vector2(9,-3.5f),new Vector2(-6,-3.5f),new Vector2(6,-3.5f),new Vector2(-3,-3.5f),new Vector2(3,-3.5f),new Vector2(0,-3.5f),new Vector2(-7,2.8f),new Vector2(0,3.4f),new Vector2(7,2.8f)}}};game.level.lesson="MIMO Y PIPRO / DEFIENDAN EL SALON\nEl rey tiembla de miedo junto a su trono.\nEl lazo entre ambos hace dano.\nPractiquen y protejan al rey de las tres oleadas.";hostFactory();}
  }
 }
 void Start(){if(Stage==1)GameDuoNeonPortal.Install(neon);if(Stage==0&&game&&!game.Started)game.Begin();if(Stage==0&&!GameDuoCinema.IntroPlayed)GameDuoCinema.Play(new[]{0},null);}
 void hostFactory(){if(!FindAnyObjectByType<TutorialFactory>())gameObject.AddComponent<TutorialFactory>();}
 void Update(){
  if(menu||finishing||GameDuoCinema.Busy)return;
  bool won=game&&game.Victory&&!game.Practice || neon&&neon.Completed || mechanisms&&mechanisms.Completed || penguins&&penguins.Won;
  if(!won)return;
  finishing=true;
  finishAt=Time.time+2;
  PlayerPrefs.SetInt(SaveKey,Mathf.Max(PlayerPrefs.GetInt(SaveKey,0),Mathf.Min(Stage+1,Scenes.Length-1)));
  PlayerPrefs.Save();
 }
 public static bool TryAdvance()=>Active;
 public static bool TryMenu(){if(!Active)return false;if(!MissionWon&&!GameDuoCinema.Busy)Instance.OpenMenu();return true;}
 internal void ContinueMission(){
  if(!ResultsReady||GameDuoCinema.Busy)return;
  int[][] movies={new[]{1,2},new[]{3},new[]{4},new[]{5,6},new[]{7,8}};
  if(Completed)ReturnToMainMenu(movies[Stage]);
  else GameDuoCinema.Play(movies[Stage],Scenes[Stage+1]);
 }
 void ReplayMission(){
  if(!ResultsReady||GameDuoCinema.Busy)return;
  GameDuoCinema.Play(System.Array.Empty<int>(),Scenes[Stage]);
 }
 void ReturnToMainMenu(int[] movies){
  if(!ResultsReady||GameDuoCinema.Busy)return;
  GameDuoLan.Local();
  GameDuoCinema.Play(movies,GameDuoMainMenu.ScenePath);
 }
 void Load(int stage){
  Time.timeScale=1;menu=false;CandyCampaign.ArrivalPending=false;
  GameDuoCinema.Play(stage==0?new[]{0}:new int[0],Scenes[stage]);
 }
 void OpenMenu(){if(menu)return;menu=true;previousScale=Time.timeScale;Time.timeScale=0;}
 void Resume(){menu=false;Time.timeScale=previousScale;}
 void OnGUI(){
  if(GameDuoCinema.Busy||!menu&&!ResultsReady)return;
  int depth=GUI.depth;
  GUI.depth=-100;
  var matrix=GUI.matrix;
  GUI.matrix=Matrix4x4.identity;
  if(ResultsReady)CoopPromptUI.Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.015f,.01f,.045f,.8f));
  GUI.matrix=CoopPromptUI.FitSafeArea();
  if(ResultsReady)DrawResults();
  else{
   GUI.Box(new Rect(85,65,470,240),GUIContent.none);
   var style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,wordWrap=true,fontSize=18};
   GUI.Label(new Rect(100,80,440,60),"Capítulo "+(Stage+1)+" de "+Scenes.Length+"\n"+Titles[Stage],style);
   if(GUI.Button(new Rect(130,145,380,30),"Continuar"))Resume();
   if(GUI.Button(new Rect(130,183,380,30),"Reintentar capítulo"))Load(Stage);
   if(GUI.Button(new Rect(130,221,380,30),"Volver al tutorial"))Load(0);
   int unlocked=UnlockedStage;
   if(unlocked>Stage&&GUI.Button(new Rect(130,259,380,30),"Ir al último capítulo desbloqueado"))Load(unlocked);
  }
  GUI.matrix=matrix;
  GUI.depth=depth;
 }
 void DrawResults(){
  CoopPromptUI.Panel(new Rect(107,30,426,300),CoopPromptUI.Mint);
  CoopPromptUI.Hero(new Rect(257,49,42,48),0);
  CoopPromptUI.Hero(new Rect(344,61,31,36),1);
  CoopPromptUI.Line(new Vector2(296,80),new Vector2(345,86),2,CoopPromptUI.Mint);
  CoopPromptUI.Text(new Rect(127,103,386,35),"Victoria",23,CoopPromptUI.Mint);
  CoopPromptUI.Text(new Rect(127,138,386,24),Completed?"Mimo y Pipro recuperaron su hogar":Titles[Stage],13);
  if(CoopPromptUI.Button(new Rect(178,176,284,40),"Continuar",CoopPromptUI.Mint))ContinueMission();
  if(CoopPromptUI.Button(new Rect(178,224,284,36),"Volver a Jugar",Color.white))ReplayMission();
  if(CoopPromptUI.Button(new Rect(178,267,284,36),"Volver al Menú",Color.white))ReturnToMainMenu(System.Array.Empty<int>());
 }
 void OnDestroy(){if(menu)Time.timeScale=previousScale;if(game&&game.level)Destroy(game.level);if(Instance==this)Instance=null;}
}
}



