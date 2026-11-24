using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace CoopBeam {
// Persistent overlay owns both movie playback and asynchronous chapter changes.
public sealed class GameDuoCinema : MonoBehaviour {
 static GameDuoCinema instance;
 public static bool Busy => instance && instance.busy;
 public static bool IntroPlayed { get; private set; }
 bool firstFrame; bool busy, failed, ended, showingMovie; float opacity; string error;
 VideoPlayer player; AudioSource audioOutput; RenderTexture target;
 const float FadeDuration=0.35f, BlackHoldDuration=0.08f;
 bool holdBlackBackground, canSkip, skipMovies, loadingScene;

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
 static void ResetSession(){instance=null;IntroPlayed=false;}

 public static void Play(int[] movies, string scene, Action complete=null) {
  if(Busy)return;
  if(!instance){var host=new GameObject("GameDuo / Cine y transiciones");instance=host.AddComponent<GameDuoCinema>();DontDestroyOnLoad(host);}
  IntroPlayed=true;
  instance.busy=true;
  instance.StartCoroutine(instance.Sequence(movies,scene,complete));
 }

 IEnumerator Fade(float to, float duration=FadeDuration, bool fadeAudio=false) {
  float from=opacity;
  for(float t=0;t<duration;t+=Time.unscaledDeltaTime){
   float p=duration>0?Mathf.Clamp01(t/duration):1f;
   opacity=Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,p));
   if(fadeAudio&&audioOutput)audioOutput.volume=Mathf.Clamp01(1f-opacity);
   yield return null;
  }
  opacity=to;
  if(fadeAudio&&audioOutput)audioOutput.volume=Mathf.Clamp01(1f-opacity);
 }

 IEnumerator Sequence(int[] movies,string scene,Action complete) {
  busy=true; skipMovies=false;canSkip=movies.Length>0;
  float scale=Time.timeScale; bool paused=AudioListener.pause;
  Time.timeScale=0; AudioListener.pause=true;

  yield return Fade(1, FadeDuration);

  holdBlackBackground=true;

  var library=Resources.Load<GameDuoMovieLibrary>("GameDuoMovies");
  bool isFirstMovie=true;

  foreach(int index in movies) {
   if(isFirstMovie){
    yield return new WaitForSecondsRealtime(BlackHoldDuration);
    isFirstMovie=false;
   } else {
    yield return new WaitForSecondsRealtime(0.04f);
   }

   if(skipMovies)break;
   failed=false;ended=false;error=null;
   if(index<0||index>=9
#if !UNITY_WEBGL || UNITY_EDITOR
    ||!library||library.clips==null||index>=library.clips.Length||!library.clips[index]
#endif
   ){
    failed=true;error="No se encontró el video "+(index+1);
   } else {
    if(!player){
     player=gameObject.AddComponent<VideoPlayer>();player.playOnAwake=false;player.isLooping=false;
     #if UNITY_ANDROID && !UNITY_EDITOR
     player.renderMode=VideoRenderMode.APIOnly;
     player.timeUpdateMode=VideoTimeUpdateMode.UnscaledGameTime;
#else
     player.renderMode=VideoRenderMode.RenderTexture;
#endif
     player.audioOutputMode=VideoAudioOutputMode.AudioSource;
     audioOutput=gameObject.AddComponent<AudioSource>();audioOutput.playOnAwake=false;audioOutput.ignoreListenerPause=true;audioOutput.spatialBlend=0;audioOutput.mute=false;audioOutput.volume=1;
     player.errorReceived+=(p,message)=>{failed=true;error=message;Debug.LogError("GameDuo video: "+message);};
     player.loopPointReached+=p=>ended=true;
     player.sendFrameReadyEvents=true;player.waitForFirstFrame=true;
     player.frameReady+=(p,frame)=>firstFrame=true;
    }
#if UNITY_WEBGL && !UNITY_EDITOR
    player.source=VideoSource.Url;
    player.url=Application.streamingAssetsPath+"/GameDuoMovies/"+(index==8?"the%20final":(index+1).ToString())+".mp4";
    player.audioOutputMode=VideoAudioOutputMode.Direct;
    int clipW=1920,clipH=1080;
#else
    player.clip=library.clips[index];
    int clipW=Mathf.Max(16,(int)player.clip.width);
    int clipH=Mathf.Max(16,(int)player.clip.height);
#endif
#if !UNITY_ANDROID || UNITY_EDITOR
    if(!target||target.width!=clipW||target.height!=clipH){
     if(target){player.targetTexture=null;target.Release();Destroy(target);}
#if UNITY_IOS && !UNITY_EDITOR
     target=new RenderTexture(clipW,clipH,0,UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
#else
     target=new RenderTexture(clipW,clipH,0);
#endif
     target.Create();
     var prev=RenderTexture.active;RenderTexture.active=target;GL.Clear(true,true,Color.black);RenderTexture.active=prev;
    }
    player.targetTexture=target;
#endif
#if !UNITY_WEBGL || UNITY_EDITOR
    player.controlledAudioTrackCount=(ushort)Mathf.Min(1,(int)player.clip.audioTrackCount);
    if(player.clip.audioTrackCount>0){player.EnableAudioTrack(0,true);player.SetTargetAudioSource(0,audioOutput);}
    else Debug.LogWarning("GameDuo: el video no contiene pista de audio: "+player.clip.name);
#endif
    player.Prepare();float deadline=Time.realtimeSinceStartup+30;
    while(!skipMovies&&!player.isPrepared&&!failed&&Time.realtimeSinceStartup<deadline)yield return null;
    if(!skipMovies&&!player.isPrepared){failed=true;error="No se pudo preparar el video.";}
   }

   if(!failed&&!skipMovies){
    firstFrame=false;audioOutput.volume=1;player.Play();float frameDeadline=Time.realtimeSinceStartup+15;
    while(!skipMovies&&!firstFrame&&!failed&&!ended&&Time.realtimeSinceStartup<frameDeadline){
#if UNITY_ANDROID && !UNITY_EDITOR
     if(player.texture&&player.frame>=0){firstFrame=true;break;}
#endif
     yield return null;
    }
    if(!skipMovies&&!firstFrame){failed=true;error="El video no pudo iniciar.";}
    if(!skipMovies&&firstFrame&&!failed){
     showingMovie=true;
     yield return Fade(0, 0.28f);
     float fadeOutTime=0.32f;
     while(!skipMovies&&!ended&&!failed&&(player.length<=1f||player.time<player.length-fadeOutTime))yield return null;
     if(!skipMovies)yield return Fade(1, fadeOutTime, true);
     while(!skipMovies&&!ended&&!failed&&player.isPlaying)yield return null;
    }
   }
   opacity=1;
   if(failed&&!skipMovies){Debug.LogError(error);while(failed&&!skipMovies)yield return null;}
   if(player)player.Stop();showingMovie=false;
  }
  canSkip=false;failed=false;

  yield return new WaitForSecondsRealtime(BlackHoldDuration);

  if(scene!=null){
   LinkPowers.ResetStoredCharge();CandyCampaign.ArrivalPending=false;
   loadingScene=true;
   var load=SceneManager.LoadSceneAsync(scene);
   while(!load.isDone)yield return null;
   yield return null;
   yield return null;
  }

  loadingScene=false;
  complete?.Invoke();
  holdBlackBackground=scene==null&&complete!=null;

  yield return Fade(0, FadeDuration);

  var tutorial=FindAnyObjectByType<GameDuoTutorial>();
  Time.timeScale=tutorial&&tutorial.AwaitingConfirmation?0:scene!=null?1:scale;
  AudioListener.pause=paused;
  busy=false;
 }

#if UNITY_IOS && !UNITY_EDITOR
 GameDuoIosImageOverlay imageOverlay;
 bool ShowMovieCanvas => showingMovie && !skipMovies && !failed && target;
 void Awake(){imageOverlay=new GameDuoIosImageOverlay(transform,true);}
#endif

 void LateUpdate(){
  if(busy)Time.timeScale=0;
#if UNITY_IOS && !UNITY_EDITOR
  imageOverlay.Refresh(target,ShowMovieCanvas,opacity);
#endif
 }

 void OnGUI(){
  if(!busy&&!holdBlackBackground)return;
  if(!busy&&(!GameDuoCampaign.Instance||!GameDuoCampaign.Instance.Completed))return;
  var oldMatrix=GUI.matrix;var oldColor=GUI.color;int oldDepth=GUI.depth;
  GUI.depth=busy?-10000:-99;GUI.matrix=Matrix4x4.identity;GUI.color=Color.white;
  var screen=new Rect(0,0,Screen.width,Screen.height);
  bool canvasShowing=false;
#if UNITY_IOS && !UNITY_EDITOR
  canvasShowing=ShowMovieCanvas;
#endif
  if(!canvasShowing&&(holdBlackBackground||showingMovie))GUI.DrawTexture(screen,Texture2D.blackTexture,ScaleMode.StretchToFill,false);
  if(!busy){GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.depth=oldDepth;return;}
#if UNITY_ANDROID && !UNITY_EDITOR
  Texture movie=player?player.texture:null;
#else
  Texture movie=target;
#endif
  if(!canvasShowing&&showingMovie&&movie)GUI.DrawTexture(screen,movie,ScaleMode.ScaleToFit,false);
  if(!canvasShowing&&opacity>0.002f){
   GUI.color=new Color(1,1,1,Mathf.Clamp01(opacity));
   GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.blackTexture);
  }
  GUI.color=Color.white;
  if(loadingScene){GUI.matrix=CoopPromptUI.FitSafeArea();CoopPromptUI.Loading();GUI.matrix=Matrix4x4.identity;}
  if(canSkip&&!skipMovies){
   Rect safe=Screen.safeArea;
   if(safe.width<=0||safe.height<=0)safe=new Rect(0,0,Screen.width,Screen.height);
   float scale=GameDuoMobileInput.Scale;
   GUI.matrix=Matrix4x4.TRS(new Vector3(safe.xMax,Screen.height-safe.yMax),Quaternion.identity,new Vector3(scale,scale,1));
   if(CoopPromptUI.Button(new Rect(-158,16,142,56),"Omitir",CoopPromptUI.Mint))skipMovies=true;
   GUI.matrix=Matrix4x4.identity;
  }
  if(failed&&!skipMovies){CoopPromptUI.Text(new Rect(30,Screen.height/2-40,Screen.width-60,50),"No se pudo reproducir la cinemática.",16);if(GUI.Button(new Rect(Screen.width/2-100,Screen.height/2+20,200,40),"Continuar"))failed=false;}
  GUI.Box(new Rect(0,0,Screen.width,Screen.height),GUIContent.none,GUIStyle.none);
  GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.depth=oldDepth;
 }

 void OnDestroy(){if(busy){Time.timeScale=1;AudioListener.pause=false;}if(target){target.Release();Destroy(target);}if(instance==this)instance=null;}
}
}
