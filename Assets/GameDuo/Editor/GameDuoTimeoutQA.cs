using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CoopBeam;

[InitializeOnLoad]
public static class GameDuoTimeoutQA {
 const string Key="GameDuo.TimeoutQA";
 static GameDuoTimeoutQA(){EditorApplication.playModeStateChanged+=Changed;}
 [MenuItem("GameDuo/QA/Validar derrota por tiempo")]
 public static void Run(){
  EditorSceneManager.OpenScene(GameDuoCampaign.Scenes[1]);
  SessionState.SetBool(Key,true);
  EditorApplication.EnterPlaymode();
 }
 static void Changed(PlayModeStateChange state){
  if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){
   SessionState.SetBool(Key,false);
   new GameObject("QA / timeout regression").AddComponent<GameDuoTimeoutProbe>();
  }
 }
}

public class GameDuoTimeoutProbe:MonoBehaviour {
 int failures;string report="";float deadline;
 void Check(bool pass,string label){report+=(pass?"PASS ":"FAIL ")+label+"\n";if(!pass)failures++;Debug.Log(label+": "+pass);}
 static void Field(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
 static void Property(object target,string name,object value)=>target.GetType().GetProperty(name).SetValue(target,value);
 void Update(){if(deadline>0&&Time.realtimeSinceStartup>deadline){Check(false,"QA exceeded 45 seconds");Finish();}}
 IEnumerator Start(){
  DontDestroyOnLoad(gameObject);deadline=Time.realtimeSinceStartup+45;
  yield return null;
  var heroes=Resources.Load<PixelAnimationSet>("GameDuoControlHeroes");Check(heroes&&heroes.frames.Length>=2&&heroes.frames[0]&&heroes.frames[1],"Main menu hero sprites are available");
  var neon=FindAnyObjectByType<NeonLevel>();
  // Solved progress must never be recovered after a round timeout.
  neon.crossed[0].Set(true);neon.crossed[0].target.Set(true);
  Field(neon,"roundStarted",true);Property(neon,"Checkpoint",1);Property(neon,"RoundRemaining",.01f);
  yield return new WaitForSeconds(.1f);
  Check(neon.TimedOut&&!neon.game.Running&&!neon.Completed,"Neon round timeout stops play as defeat");
  neon.ResetTogether();Check(neon.TimedOut&&!neon.game.Running,"Checkpoint recovery cannot bypass timeout defeat");
  yield return new WaitForSeconds(2.2f);
  Check(SceneManager.GetActiveScene().path==GameDuoCampaign.Scenes[1],"Neon timeout never advances campaign");
  neon.game.Restart();yield return null;yield return null;
  neon=FindAnyObjectByType<NeonLevel>();
  Check(!neon.TimedOut&&neon.Checkpoint==0&&neon.CrossedCount==0&&!neon.RelayDone&&!neon.Restored&&!neon.Collected&&neon.RoundRemaining==60,"Neon restart clears all progress and restores timer");
  Field(neon,"roundStarted",true);Property(neon,"CoreStep",1);Field(neon,"deadline",Time.time-.1f);
  yield return new WaitForSeconds(.1f);
  Check(neon.TimedOut&&!neon.game.Running,"Core stability timeout also causes defeat");
  SceneManager.LoadScene(GameDuoCampaign.Scenes[3]);yield return null;yield return null;
  var round=FindAnyObjectByType<IcePenguinRound>();
  Property(round,"Started",true);Field(round,"intro",0f);
  var birds=(Array)round.GetType().GetField("birds",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(round);
  var first=birds.GetValue(0);first.GetType().GetField("p").SetValue(first,new Vector2(0,-3.5f));
  yield return new WaitForSeconds(.1f);Check(round.Rescued>0&&!round.Won,"Penguin fixture includes partial rescue");
  Property(round,"Remaining",.01f);yield return new WaitForSeconds(.1f);
  Check(round.TimedOut&&round.Finished&&!round.Won,"Penguin timeout is defeat, never victory");
  yield return new WaitForSeconds(2.3f);
  Check(SceneManager.GetActiveScene().path==GameDuoCampaign.Scenes[3]&&!GameDuoCinema.Busy,"Penguin timeout cannot advance campaign after transition delay");
  round.ResetRound();yield return null;
  Check(round.Rescued==0&&round.Remaining==60&&!round.Finished&&!round.Started,"Penguin restart resets counter, timer and start gate");
  birds=(Array)round.GetType().GetField("birds",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(round);
  int active=0;foreach(var bird in birds){var type=bird.GetType();var art=(SpriteRenderer)type.GetField("art").GetValue(bird);if(!(bool)type.GetField("saved").GetValue(bird)&&art.gameObject.activeSelf)active++;}
  Check(active==IcePenguinRound.FlockSize,"Restart restores all sixteen penguins, including previously rescued birds");
  Property(round,"Started",true);Field(round,"intro",0f);
  foreach(var bird in birds)bird.GetType().GetField("p").SetValue(bird,new Vector2(0,-3.5f));
  yield return new WaitForSeconds(.1f);Check(round.Won&&round.Rescued==16&&!round.TimedOut,"Full rescue still counts as success");
  Finish();
 }
 void Finish(){deadline=0;Directory.CreateDirectory("Logs");File.WriteAllText("Logs/TimeoutQA.txt",report+"Failures: "+failures+"\n");if(Application.isBatchMode)EditorApplication.Exit(failures==0?0:1);else EditorApplication.isPlaying=false;}
}
