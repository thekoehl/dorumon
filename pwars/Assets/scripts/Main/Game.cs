using System.Linq;
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
public enum GameMode { ZombieSurive, TeamZombieSurvive, DeathMatch, TeamDeathMatch }

public class Game : Base,IWindow
{
    new public Player[] players = new Player[36];
    public List<Shared> shareds = new List<Shared>();
    public List<Zombie> zombies = new List<Zombie>();
    public float[] timers = new float[20];
    public List<Patron> patrons = new List<Patron>();
    private float fixedDeltaTime;    
    public List<Tower> towers = new List<Tower>();
    public List<Shared> boxes = new List<Shared>();
    public List<MapItem> mapitems = new List<MapItem>();
    public IEnumerable<Zombie> AliveZombies { get { return zombies.Where(a => a.Alive == true); } }
    public IEnumerable<Zombie> deadZombies  { get { return zombies.Where(a => a.Alive == false); } }
    [FindTransform("bounds",true)]
    public GameObject bounds;
    public GameMode gameMode { get { return mapSettings.gameMode; } set { mapSettings.gameMode = value; } }    
    public new Player _localPlayer;
    [FindTransform("GameEffects",true)]
    public GameObject effects;
    [FindTransform("GameEffects/decals", true)]
    public GameObject decals;
    internal int stage;
    public float timeleft = 20;
    public bool wait;
    public bool win;
    public int RedFrags = 0, BlueFrags = 0;
    public Vector3 gravity;
    public int maxzombies = 0;
    [GenerateEnums("ParticleTypes")]
    public List<Particles> particles = new List<Particles>();
    [GenerateEnums("DecalTypes")]
    public List<Decal> decalPresets = new List<Decal>();
    public int zombiespawnindex = 0;
    public bool cameraActive { get { return _Cam.camera.gameObject.active; } }
    [FindAsset("Player")]
    public GameObject playerPrefab;
    [FindAsset]
    public AudioClip antigrav;
    [FindAsset]
    public AudioClip timewarp;    

    public override void Awake()
    {
        gravity = Physics.gravity;
        fixedDeltaTime = Time.fixedDeltaTime;        
        base.Awake();
        if (nick == " ") nick = "Guest " + UnityEngine.Random.Range(0, 999);        
        _Level = Level.z4game;
        Debug.Log("cmdserver:" + _Loader.cmd.Contains("client"));
        print("mapSettings.host " + mapSettings.host);
        
        if (Network.peerType == NetworkPeerType.Disconnected)
            if ((mapSettings.host && Application.isEditor) || _Loader.cmd.Contains("server"))
            {

                Network.InitializeServer(mapSettings.maxPlayers, mapSettings.port, false);
            }
            else
            {
                //if (_Loader.cmd.Length == 2)
                //{
                //    Debug.Log("added IPADDRESS: " + _Loader.cmd[2]);
                //    mapSettings.ipaddress.Union(new string[] { _Loader.cmd[2] });
                //}
                Network.Connect(mapSettings.ipaddress, _ServersWindow.Port);
            }
        else
            foreach (Base o in Component.FindObjectsOfType(typeof(Base))) o.SendMessage("Enable", SendMessageOptions.DontRequireReceiver);
        
    }

    protected override void Enable()
    {        
        RPCWriteMessage("Player Connected: " + nick);
        SendGameSettings();
        ((GameObject)Network.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity, (int)GroupNetwork.Player)).GetComponent<Player>();
        base.Enable();
    }
    private void clearObjects(string name)
    {

        foreach (var spwn in GameObject.FindGameObjectsWithTag(name))
            foreach (var a in spwn.GetComponents<Component>())
                if (!(a is Transform))
                    DestroyImmediate(a);

    }
    public override void Init()
    {
        particles = new List<Particles>(FindObjectsOfType(typeof(Particles)).Cast<Particles>());
    }
    protected override void Start()
    {
    }
    
    bool chatEnabled;

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Return))
        {
            chatEnabled = !chatEnabled;
            Screen.lockCursor = !chatEnabled;
        }
        if (chatEnabled)
        {
            _GameWindow.chatInput.text += Input.inputString;
        }
        else if (_GameWindow.chatInput.text != "")
        {
            RPCSendChantMessage(_GameWindow.chatInput.text, _localPlayer.OwnerID);

            _GameWindow.chatInput.text = "";
        }
        if (sendto != null) Debug.Log("warning,sendto is not null");
        timeleft -= Time.deltaTime / 60;

        if (Input.GetKeyDown(KeyCode.P)) RPCPause();

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            _GameStatsWindow.Show(this);
            lockCursor = true;
        }
        if (Input.GetKeyUp(KeyCode.Tab))
            _GameStatsWindow.Hide();

        if (_GameStatsWindow.enabled)
        {
            _GameStatsWindow.PlayerStats = "";
            string table = GenerateTable(_GameStatsWindow.PlayerStatsTitle);
            foreach (Player pl in players)
                if (pl != null)
                    _GameStatsWindow.PlayerStats += string.Format(table, "", pl.nick, pl.team, pl.score, pl.frags, pl.deaths, pl.fps, pl.ping) + "\r\n";
        }

        if (Input.GetMouseButtonDown(1)) lockCursor = !lockCursor;

        if (!_Loader.dontcheckwin) CheckWin();

        if (mapSettings.TeamZombiSurvive || mapSettings.ZombiSurvive)
            ZUpdate();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _GameMenuWindow.Toggle(this);
        }

        if (_TimerA.TimeElapsed(10000))
            RPCPingFps(Network.player.GetHashCode(),
                (Network.connections.Length > 0 && Network.isClient ? Network.GetLastPing(Network.connections[0]) : 0),
                _GameWindow.fps);

    }
    void onMenu() { _GameMenuWindow.Show(this); }
    public void Action(string s, params object[] p)
    {
        if (s == "onMenu")
            _GameMenuWindow.Show(this);
        if (s == "onIrcChatButton")
            _IrcChatWindow.Show(this);
        if (s == "onTeams")
            _TeamSelectWindow.tabImages = _TeamSelectWindow.iTeams;
        if (s == "onOptions")
            _SettingsWindow.Show(_Loader);
        if (s == "onDisconnect")
            Network.Disconnect();
        if (s == "onTeamSelectButton")
            _TeamSelectWindow.Show(this);
        if (s == "onScoreBoard")
            _GameStatsWindow.Show(this);
        if (s == "onTeamSelect")
        {
            _TeamSelectWindow.Hide();
            _localPlayer.team = (mapSettings.DM || mapSettings.ZombiSurvive) ? Team.None : (Team)_TeamSelectWindow.iTeams;
            if (!_localPlayer.spawned) _localPlayer.RPCSetAlive(true);
            lockCursor = true;
        }
    }

    public void RPCGameSettings(string version, int gameMode, int frags, float timelimit,int stage) { CallRPC("GameSettings", version, gameMode, frags, timelimit,stage); }
    [RPC]
    public void GameSettings(string version, int gameMode, int frags, float timelimit,int stage)
    {
        mapSettings.gameMode = (GameMode)gameMode;
        timeleft = mapSettings.timeLimit = timelimit;
        this.stage = mapSettings.stage = stage;
        _TeamSelectWindow.tabGameType = (int)mapSettings.gameMode;
        _TeamSelectWindow.Fraglimit = mapSettings.fragLimit = frags;
        if (!mapSettings.Team) _TeamSelectWindow.enabledTeamsView = false;
        _TeamSelectWindow.Show(this);
    }
    public void OnPlayerConnected(NetworkPlayer np)
    {
        Debug.Log("On Player Connected " + GameObject.FindObjectsOfType(typeof(Zombie)).Length);
        sendto = np;
        RPCSetTimeLeft(timeleft);
        SendGameSettings();
        if (mapSettings.zombi && stage != 0) RPCNextStage(stage);        
        var sorted = GameObject.FindObjectsOfType(typeof(Player))
        .Union(GameObject.FindObjectsOfType(typeof(Gun)))
        .Union(GameObject.FindObjectsOfType(typeof(Destroible)))
        .Union(GameObject.FindObjectsOfType(typeof(Box)))
        .Union(GameObject.FindObjectsOfType(typeof(MapItem)))
        .Union(GameObject.FindObjectsOfType(typeof(Base)));
        Debug.Log(sorted.Where(a => a is Zombie).Count());
        foreach (Base b in sorted)
            b.OnPlayerConnectedBase(np);
        sendto = null;
    }

    private void SendGameSettings()
    {
        RPCGameSettings(_Console.version, (int)gameMode, mapSettings.fragLimit, mapSettings.timeLimit, mapSettings.stage);
    }
    public void RPCSetTimeLeft(float time) { CallRPC("SetTimeLeft",time); }
    [RPC]
    public void SetTimeLeft(float time)
    {
        timeleft = time;
    }
    public void RPCSetGravityBomb(bool enable) { CallRPC("SetGravityBomb",enable); }
    [RPC]
    public void SetGravityBomb(bool enable)
    {
        Physics.gravity = enable ? new Vector3(0, 0.1f, 0) : gravity;
        if (enable)
        {
            foreach (Rigidbody a in FindObjectsOfType(typeof(Rigidbody)))
                if (!a.isKinematic)
                {
                    a.AddForce(Vector3.up * 100 * Random.value * fdt);
                    a.angularVelocity = Random.insideUnitSphere * 100;
                }
            _Cam.Vingetting.enabled = true;
            root.audio.clip = antigrav;
            root.audio.Play();
        }
        else
        {
            root.audio.Stop();
            _Cam.Vingetting.enabled = false;
        }
    }
    public void RPCSetTimeBomb(float timescale) { CallRPC("SetTimeBomb",timescale); }
    [RPC]
    public void SetTimeBomb(float timescale)
    {
        
        Time.timeScale = timescale;
        Time.fixedDeltaTime = fixedDeltaTime * timescale;
        foreach (AudioSource a in FindObjectsOfTypeIncludingAssets(typeof(AudioSource)))
            a.pitch = Time.timeScale;
        if (Time.timeScale == 1)
        {
            _Cam.Vingetting.enabled = false;
            root.audio.Stop();
        }
        else
        {
            root.audio.clip = timewarp;
            root.audio.Play();
            _Cam.Vingetting.enabled = true;
        }

    }
    public void RPCPause() { CallRPC("Pause"); }
    [RPC]
    public void Pause()
    {
        Debug.Break();
    }
    
    bool HasAny() { return players.Count(a => a != null && a.spawned) > 0; }
    [FindAsset("Zombie")]
    public GameObject ZombiePrefab;
    private void ZUpdate()
    {
        if (_TimerA.TimeElapsed(500) && HasAny() && Network.isServer)
        {
            if (zombiespawnindex < maxzombies)
            {

                if (zombiespawnindex < zombies.Count)
                {
                    zombies[zombiespawnindex].CreateZombie(stage);
                }
                else
                {
                    GameObject t = (GameObject)Network.Instantiate(ZombiePrefab, Vector3.zero, Quaternion.identity, (int)GroupNetwork.Zombie);
                    Zombie z = (t).GetComponent<Zombie>();
                    z.CreateZombie(stage);
                }
                wait = false;
                zombiespawnindex++;
            }
            if (AliveZombies.Count() == 0 && zombiespawnindex == maxzombies && !wait)
            {

                wait = true;
                _TimerA.AddMethod(2000, delegate { RPCNextStage(stage + 1); });
            }
        }
    }
    
    public void RPCWriteMessage(string s) { CallRPC("WriteMessage",s); }
    [RPC]
    public void WriteMessage(string s)
    {
        _GameWindow.AppendSystemMessage(s);
    }


    public void RPCPingFps(int id, int ping, int fps) { CallRPC("PingFps", id, ping, fps); }
    [RPC]    
    public void PingFps(int id, int ping, int fps)
    {
        players[id].fps = fps;
        players[id].ping = ping;        
    }
    
    
    [FindAsset("nextLevel")]
    public AudioClip[] stageSound;
    public void RPCNextStage(int stage) { CallRPC("NextStage",stage); }
    [RPC]
    public void NextStage(int stage)
    {
        Debug.Log("Next Stage"+stage);
        PlayRandSound(stageSound);
        this.stage = stage;
        maxzombies = mapSettings.fragLimit + stage;
        zombiespawnindex = 0;
        _Cam.LevelText.text = "Stage " + stage;
        _Cam.LevelText.animation.Play();
        foreach (Player p in players)
            if (p != null)
                if (!p.Alive && !p.spawned && stage == 1)
                    p.RPCSpawn();
    }
    public void RPCSendChantMessage(string msg,int userid) { CallRPC("ChantMessage",msg,userid); }
    List<string> chat = new List<string>();
    [RPC]
    public void ChantMessage(string s, int id)
    {
        string d= players[id] + ": " + s;
        chat.Add(d);
        _GameWindow.chatOutput.text = string.Join("\r\n", chat.TakeLast(7).ToArray());
    }
    
    //public void Spectator()
    //{
    //    lockCursor = true;
    //    _TeamSelectWindow.Hide();
    //    if (_localPlayer != null && _localPlayer.car == null)
    //    {                        
    //        _localPlayer.RPCSetTeam((int)Team.None);
    //        _localPlayer.RPCSetAlive(false);
    //    }
    //}
    void OnDisconnectedFromServer(NetworkDisconnection nd)
    {
        _Loader.LoadLevel("Menu", _Loader.lastLevelPrefix + 1);
    }
    void OnPlayerDisconnected(NetworkPlayer player)
    {
        int playerid = player.GetHashCode();
        RPCWriteMessage(players[playerid].nick + " Player Leaved" + player);
        foreach (Shared box in GameObject.FindObjectsOfType(typeof(Shared)))
            if (!(box is Player))
            {
                if (box.selected == playerid)
                    box.RPCResetOwner();

                foreach (NetworkView nw in box.GetComponents<NetworkView>())
                    if (nw.owner.GetHashCode() == playerid) RPCDestroy(nw.viewID);
            }

        Network.DestroyPlayerObjects(player);
        Network.RemoveRPCs(player);
    }

    public void RPCDestroy(NetworkViewID v) { CallRPC("Destroy", v); }
    [RPC]
    public void Destroy(NetworkViewID v)
    {
        
        NetworkView nw = NetworkView.Find(v);
        nw.enabled = false;
        Component.Destroy(nw);
    }
    
    void CheckWin()
    {
        if (_TimerA.TimeElapsed(1000))
        {
            BlueFrags = RedFrags = 0;
            foreach (Player pl in TP(Team.Red))
                RedFrags += pl.frags;

            foreach (Player pl in TP(Team.Blue))
                BlueFrags += pl.frags;

            if (Network.isServer && !win)
            {

                if (mapSettings.DM)
                    DMCheck();
                else if (mapSettings.TeamZombiSurvive)
                    ZombiTDMCheck();
                else if (mapSettings.TDM)
                    TDMCheck();
                else if (mapSettings.ZombiSurvive)
                    ZombieSuriveCheck();
            }
        }
    }
    

    private void ZombieSuriveCheck()
    {
        int live = 0;
        foreach (Player p in players)
            if (p != null && (p.Alive || !p.spawned)) live++;
        if (live == 0 && HasAny() || TimeEnd)
        {
            RPCWriteMessage(String.Format("You survived until {0} level.", stage));
            ShowEndStats();
        }
    }
    bool TimeEnd { get { return timeleft < 0; } }
    private void DMCheck()
    {
        foreach (Player pl in players)
            if (pl != null && pl.OwnerID != -1)
            {
                if (!win && pl.frags >= mapSettings.fragLimit || TimeEnd)
                {
                    RPCWriteMessage(pl.nick + " Win");
                    ShowEndStats();
                }
            }
    }
    private void TDMCheck()
    {
      

        if ((BlueFrags >= mapSettings.fragLimit || RedFrags >= mapSettings.fragLimit || TimeEnd))
        {                        
            RPCWriteMessage((BlueFrags > RedFrags ? "Red" : "Blue") + " Team Win");
            ShowEndStats();
        }
    }
    void ShowEndStats()
    {
        win = true;
        _GameStatsWindow.Show(this);
        _TimerA.AddMethod(5000, WinGame);
    }
    private void ZombiTDMCheck()
    {
        bool BlueteamLive = false, RedteamLive = false;
        int rcount = 0, bcount = 0;
        foreach (Player p in TP(Team.Blue))
        {
            rcount++;
            if (p.Alive) BlueteamLive = true;
        }
        foreach (Player p in TP(Team.Red))
        {
            bcount++;
            if (p.Alive) RedteamLive = true;
        }

        if (Network.isServer && rcount > 0 && bcount > 0 && (!RedteamLive || !BlueteamLive) || TimeEnd)
        {
            RPCWriteMessage((!RedteamLive ? "Blue" : "Red") + " Team Win");
            ShowEndStats();
        }
    }
    private void WinGame()
    {
        print(pr);
        Debug.Break();
        Network.Disconnect();
    }
    public void AddDecal(DecalTypes t, Vector3 pos, Vector3 normal, Transform addto)
    {
        if (_SettingsWindow.Decals)
        {

            Decal d = decalPresets[(int)t];
            d.mesh.renderer.material = d.mat;
            d.mesh.transform.localScale = Vector3.one * d.scale;
            var g = ((GameObject)Instantiate(d.mesh, pos, Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), normal) * Quaternion.LookRotation(normal)));
            Destroy(g, 10);
            g.transform.parent = addto;
        }
    }
    
}
public enum GroupNetwork { PlView, RPCSetID, Default, Shared, staticfield, Spawn, Nick, Gun, SetMovement, Player, Zombie,Tower }