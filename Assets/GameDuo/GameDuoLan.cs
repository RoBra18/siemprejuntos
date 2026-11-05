using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace CoopBeam {
// The host simulates the complete game. The second device sends controls only.
public sealed class GameDuoLan : MonoBehaviour {
 public enum LanMode { Local, Host, Guest }
 public static LanMode Mode { get; private set; }
 public static GameDuoLan Instance { get; private set; }
 public const int Port=27841;
 public static bool Connected=>Instance&&Instance.connected;
 public static string Status=>Instance?Instance.status:"Sin conexión";
 public static string Address=>Instance?Instance.address:"";
 public static GameDuoMobileInput.State Remote {get{if(!Instance||!Instance.connected)return default;lock(Instance.sync)return Instance.remote;}}
 TcpListener listener;TcpClient socket;Thread worker;volatile bool stopping,connected;string status="Sin conexión",address="";
 GameDuoMobileInput.State remote, outgoing;
 readonly object sync=new object();long lastPacket;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
 static void ResetStatics(){Instance=null;Mode=LanMode.Local;}
 public static void Ensure(){if(Instance)return;var go=new GameObject("GameDuo / Red local");Instance=go.AddComponent<GameDuoLan>();if(Application.isPlaying)DontDestroyOnLoad(go);}
 public static void StartHost(){Ensure();Instance.StopNetwork();Mode=LanMode.Host;Instance.address=LocalAddress();Instance.status="Esperando al segundo móvil en "+Instance.address+":"+Port;Instance.worker=new Thread(Instance.HostLoop){IsBackground=true};Instance.worker.Start();}
 public static void Join(string ip){Ensure();Instance.StopNetwork();Mode=LanMode.Guest;Instance.address=ip.Trim();Instance.status="Conectando a "+Instance.address;Instance.worker=new Thread(Instance.GuestLoop){IsBackground=true};Instance.worker.Start();}
 public static void Local(){if(Instance)Instance.StopNetwork();Mode=LanMode.Local;}
 public static void Send(GameDuoMobileInput.State state){if(Instance){lock(Instance.sync)Instance.outgoing=state;}}
 void HostLoop(){try{listener=new TcpListener(IPAddress.Any,Port);listener.Start();while(!stopping){var pending=listener.AcceptTcpClient();if(stopping){pending.Close();break;}socket=pending;socket.NoDelay=true;connected=true;status="Segundo móvil conectado";ReadLoop(socket);connected=false;remote=default;status="Desconectado. Esperando reconexión";socket.Close();socket=null;}}catch(Exception e){if(!stopping)status="No se pudo abrir la red: "+e.Message;}finally{connected=false;try{listener?.Stop();}catch{}}}
 void GuestLoop(){try{var client=new TcpClient();socket=client;client.Connect(address,Port);client.NoDelay=true;connected=true;status="Conectado al anfitrión";using(var stream=client.GetStream()){while(!stopping&&client.Connected){GameDuoMobileInput.State state;lock(sync)state=outgoing;byte[] data=Encoding.ASCII.GetBytes(Encode(state));stream.Write(data,0,data.Length);Thread.Sleep(33);}}}catch(Exception e){if(!stopping)status="Conexión perdida: "+e.Message;}finally{connected=false;try{socket?.Close();}catch{}}}
 void ReadLoop(TcpClient client){using(var reader=new StreamReader(client.GetStream(),Encoding.ASCII)){string line;while(!stopping&&(line=reader.ReadLine())!=null){var parts=line.Split(',');if(parts.Length!=6)continue;if(!float.TryParse(parts[0],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float axis))continue;lock(sync){remote=new GameDuoMobileInput.State{axis=Mathf.Clamp(axis,-1,1),jump=parts[1]=="1",down=parts[2]=="1",ultimate=parts[3]=="1",action=parts[4]=="1",dash=parts[5]=="1"};lastPacket=DateTime.UtcNow.Ticks;}}}}
 static string Encode(GameDuoMobileInput.State s)=>s.axis.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+","+(s.jump?"1":"0")+","+(s.down?"1":"0")+","+(s.ultimate?"1":"0")+","+(s.action?"1":"0")+","+(s.dash?"1":"0")+"\n";
 void Update(){if(Mode==LanMode.Host&&connected&&DateTime.UtcNow.Ticks-lastPacket>TimeSpan.TicksPerSecond/2)lock(sync)remote=default;}
 void StopNetwork(){stopping=true;connected=false;try{socket?.Close();}catch{}try{listener?.Stop();}catch{}if(worker!=null&&worker.IsAlive)worker.Join(300);worker=null;socket=null;listener=null;remote=default;stopping=false;status="Sin conexión";}
 void OnDestroy(){StopNetwork();if(Instance==this)Instance=null;}
 static string LocalAddress(){try{foreach(var ip in Dns.GetHostAddresses(Dns.GetHostName()))if(ip.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(ip))return ip.ToString();}catch{}return "IP del anfitrión";}
}
}
