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
 public static bool Paused=>Active&&Instance.menu;
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
 static void ReturnToMainMenu(int[] movies){
  if(GameDuoCinema.Busy)return;
  GameDuoLan.Local();
  GameDuoCinema.Play(movies,GameDuoMainMenu.ScenePath);
 }
 internal static void DrawDefeatActions(float y,System.Action retry){
  var energy=CampaignEnergy.Current;
  string detail=$"Energía: {energy.Count}/{CampaignEnergy.Capacity}";
  if(energy.Count<CampaignEnergy.Capacity)detail+=" · Próxima en "+energy.Countdown;
  CoopPromptUI.Text(new Rect(107,y-24,426,22),detail,12,CoopPromptUI.Amber);
  var retryButton=new Rect(107,y,200,36);
  if(energy.Count>0){
   if(CoopPromptUI.Button(retryButton,"Reintentar",CoopPromptUI.Mint)){retry();return;}
  }else if(CampaignRewardedAd.Available){
   if(CampaignRewardedAd.Busy)
    CoopPromptUI.Text(retryButton,"Preparando anuncio…",13,CoopPromptUI.Amber);
   else if(CampaignRewardedAd.RetrySeconds>0)
    CoopPromptUI.Text(retryButton,$"Sin anuncio · Reintenta en {CampaignRewardedAd.RetrySeconds} s",12,CoopPromptUI.Amber);
   else if(CoopPromptUI.Button(retryButton,CampaignRewardedAd.Failed?"Volver a cargar anuncio":"Ver anuncio · +1 energía",CoopPromptUI.Amber))
    CampaignRewardedAd.Show(Instance);
  }else{
   CoopPromptUI.Text(retryButton,"Sin energía",13,CoopPromptUI.Amber);
  }
  if(CoopPromptUI.Button(new Rect(333,y,200,36),"Volver al Menú",Color.white))
   ReturnToMainMenu(System.Array.Empty<int>());
 }
 void OpenMenu(){if(menu)return;menu=true;previousScale=Time.timeScale;Time.timeScale=0;}
 void Resume(){menu=false;Time.timeScale=previousScale;}
 void OnGUI(){
  if(GameDuoCinema.Busy)return;
  int depth=GUI.depth;
  GUI.depth=-100;
  var matrix=GUI.matrix;
  GUI.matrix=Matrix4x4.identity;
  if(ResultsReady)CoopPromptUI.Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.015f,.01f,.045f,neon?1f:.8f));
  GUI.matrix=CoopPromptUI.FitSafeArea();
  if(ResultsReady)DrawResults();
  else if(menu){
   CoopPromptUI.Panel(new Rect(167,132,306,94),new Color(.56f,.53f,.72f));
   CoopPromptUI.Text(new Rect(175,139,290,27),"Pausa",16);
   if(CoopPromptUI.Button(new Rect(179,173,134,38),"Continuar",CoopPromptUI.Mint))Resume();
   if(CoopPromptUI.Button(new Rect(320,173,142,38),"Volver al Menú",Color.white)){
    Resume();
    ReturnToMainMenu(System.Array.Empty<int>());
   }
  }else if(!finishing&&!GameDuoTutorial.BlocksGameplay&&Time.timeScale>0&&
   (game&&game.Running||penguins&&penguins.Started&&!penguins.Finished)){
   var hitArea=new Rect(595,2,42,38);
   var icon=new Rect(605,10,22,22);
   CoopPromptUI.Fill(icon,new Color(.055f,.045f,.12f,.65f));
   CoopPromptUI.Fill(new Rect(611,15,3,12),Color.white);
   CoopPromptUI.Fill(new Rect(618,15,3,12),Color.white);
   if(GUI.Button(hitArea,new GUIContent("", "Pausa"),GUIStyle.none))OpenMenu();
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

internal static class CampaignEnergy {
 public const int Capacity=6;
 const string SaveKey="GameDuo.Energy";
 static readonly long Interval=System.TimeSpan.FromMinutes(30).Ticks;

 internal readonly struct State {
  public readonly int Count;
  public readonly long Next;
  public State(int count,long next){Count=count;Next=next;}
  public string Countdown {
   get {
    var remaining=System.TimeSpan.FromTicks(System.Math.Max(0,Next-System.DateTime.UtcNow.Ticks));
    return $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
   }
  }
 }

 public static State Current=>Read(System.DateTime.UtcNow.Ticks);

 public static void Spend(){
  long now=System.DateTime.UtcNow.Ticks;
  var state=Read(now);
  if(state.Count==0)return;
  Store(new State(state.Count-1,state.Count==Capacity?now+Interval:state.Next));
 }

 public static void GrantReward(){
  var state=Current;
  if(state.Count>=Capacity)return;
  int count=state.Count+1;
  Store(new State(count,count==Capacity?0:state.Next));
 }

 static State Read(long now){
  string[] parts=PlayerPrefs.GetString(SaveKey,"").Split('|');
  if(parts.Length!=2||!int.TryParse(parts[0],out int count)||!long.TryParse(parts[1],out long next))
   return new State(Capacity,0);
  var state=Recharge(new State(count,next),now);
  if(state.Count!=count||state.Next!=next)Store(state);
  return state;
 }

 internal static State Recharge(State state,long now){
  int count=Mathf.Clamp(state.Count,0,Capacity);
  if(count==Capacity)return new State(Capacity,0);
  if(state.Next<=0||state.Next>now+Interval)return new State(count,now+Interval);
  if(now<state.Next)return new State(count,state.Next);
  long gained=1+(now-state.Next)/Interval;
  if(gained>=Capacity-count)return new State(Capacity,0);
  return new State(count+(int)gained,state.Next+gained*Interval);
 }

 static void Store(State state){
  PlayerPrefs.SetString(SaveKey,$"{state.Count}|{state.Next}");
  PlayerPrefs.Save();
 }
}
internal static class CampaignRewardedAd {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
#if UNITY_IOS && DEVELOPMENT_BUILD
 const string Unit="ca-app-pub-3940256099942544/1712485313";
#elif UNITY_IOS
 const string Unit="ca-app-pub-2875851870293518/6115765364";
#else
 const string Unit="ca-app-pub-2875851870293518/8625560397";
#endif
 public static bool Available=>true;
 public static bool Busy {get;private set;}
 public static bool Failed {get;private set;}
 static bool initialized;
 static double retryAt;
 public static int RetrySeconds=>System.Math.Max(0,(int)System.Math.Ceiling(retryAt-Time.realtimeSinceStartupAsDouble));

 public static void Show(MonoBehaviour owner){
  if(Busy||RetrySeconds>0||!owner||CampaignEnergy.Current.Count>0)return;
  Busy=true;
  Failed=false;
  if(initialized){Load(owner);return;}
  GoogleMobileAds.Api.MobileAds.RaiseAdEventsOnUnityMainThread=true;
  GoogleMobileAds.Api.MobileAds.Initialize(_=> {
   initialized=true;
   Load(owner);
  });
 }

 static void Load(MonoBehaviour owner){
  GoogleMobileAds.Api.RewardedAd.Load(Unit,new GoogleMobileAds.Api.AdRequest(),(ad,error)=>{
   if(error!=null||ad==null||!ad.CanShowAd()){
    Debug.LogWarning(error != null ? $"AdMob rewarded load failed: {error.GetDomain()} ({error.GetCode()}): {error.GetMessage()}" : "AdMob rewarded ad was not ready to show.");
    ad?.Destroy();Finish(true);return;
   }
   if(!owner||GameDuoCinema.Busy||CampaignEnergy.Current.Count>0){
    ad.Destroy();Finish(false);return;
   }
   bool rewarded=false;
   ad.OnAdFullScreenContentClosed+=()=>{ad.Destroy();Finish(false);};
   ad.OnAdFullScreenContentFailed+=error=>{
    Debug.LogWarning($"AdMob rewarded show failed: {error}");
    ad.Destroy();Finish(true);
   };
   ad.Show(_=>{
    if(rewarded)return;
    rewarded=true;
    CampaignEnergy.GrantReward();
   });
  });
 }

 static void Finish(bool failed){
  Failed=failed;
  retryAt=failed?Time.realtimeSinceStartupAsDouble+30:0;
  Busy=false;
 }
#else
 public static bool Available=>false;
 public static bool Busy=>false;
 public static bool Failed=>false;
 public static int RetrySeconds=>0;
 public static void Show(MonoBehaviour owner){}
#endif
}
}