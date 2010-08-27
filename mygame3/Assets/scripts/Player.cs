using UnityEngine;
using System.Collections;
using doru;
using System.Collections.Generic;
using System;

public enum Team : int { None, ata, def }
public class Player : IPlayer
{
    public CarController car;
    internal new Team team;
    public float flyForce = 300;
    public Transform bloodexp;
    public float force = 400;
    public int frags;
    internal float angularvel = 1000;
    public float freezedt;
    public float maxVelocityChange = 10.0f;
    public Renderer FrozenRender;
    public string Nick;
    GuiBlood blood { get { return GuiBlood._This; } }
    protected override void Start()
    {

        base.Start();
        if (networkView.isMine)
        {
            _localiplayer = _LocalPlayer = this;
            RPCSetNick(GuiConnection.Nick);
            RPCSetOwner();
            RPCSpawn();
            RPCSetTeam((int)team);
        }
    }
    [RPC]
    public void RPCSpawn()
    {
        CallRPC(true);
        Show(true);
        if(isOwner)
            RPCSelectGun(1);
        foreach (GunBase gunBase in guns)
            gunBase.Reset();
        Life = 100;
        freezedt = 0;
        transform.position = SpawnPoint();
        transform.rotation = Quaternion.identity;
    }
    int guni;

    public int fps;
    public int ping;
    [RPC]
    void RPCPingFps(int ping, int fps)
    {
        CallRPC(true, ping, fps);
        this.ping = ping;
        this.fps = fps;

    }
    //public Player serverPl
    //{
    //    get
    //    {
    //        print(Network.connections[0]);
    //        return Network.isServer ? _LocalPlayer : _Spawn.players[Network.connections[0]];
    //    }
    //}

    protected override void Update()
    {
        if (freezedt >= 0)
        {
            freezedt -= Time.deltaTime * 5;
            FrozenRender.enabled = true;
        }
        else FrozenRender.enabled = false;
        //this.transform.Find("Sphere").rotation = Quaternion.Euler(this.rigidbody.angularVelocity);


        if (_TimerA.TimeElapsed(1000) && isOwner && _Spawn.players.Count > 0 && Network.connections.Length > 0)
            RPCPingFps(Network.GetLastPing(Network.connections[0]), _Loader.fps);
        if (isOwner && lockCursor)
        {
            NextGun(Input.GetAxis("Mouse ScrollWheel"));
            if (Input.GetKeyDown(KeyCode.Alpha1))
                RPCSelectGun(0);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                RPCSelectGun(1);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                RPCSelectGun(2);
            if (Input.GetKeyDown(KeyCode.Alpha4))
                RPCSelectGun(3);
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                if (nitro > 10)
                {
                    nitro -= 10;
                    RCPJump();
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                rigidbody.velocity = new Vector3(0, rigidbody.velocity.y, 0);
                rigidbody.angularVelocity = Vector3.zero;
            }

        }
        base.Update();
    }

    [RPC]
    private void RCPJump()
    {
        CallRPC(true);
        rigidbody.AddForce(_Cam.transform.rotation * new Vector3(0, 0, 1000));
    }

    public void NextGun(float a)
    {
        if (a != 0)
        {
            transform.Find("Guns").GetComponent<AudioSource>().Play();
            if (a > 0)
                guni++;
            if (a < 0)
                guni--;
            if (guni > guns.Length - 1) guni = 0;
            if (guni < 0) guni = guns.Length - 1;
            RPCSelectGun(guni);
        }
    }
    protected virtual void FixedUpdate()
    {
        if (isOwner) LocalMove();
    }
    private void LocalMove()
    {
        if (lockCursor)
        {
            Vector3 moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = _Cam.transform.TransformDirection(moveDirection);
            moveDirection.y = 0;
            moveDirection.Normalize();
            this.rigidbody.AddTorque(new Vector3(moveDirection.z, 0, -moveDirection.x) * Time.deltaTime * angularvel);
            this.rigidbody.maxAngularVelocity = (FrozenRender.enabled || Input.GetKey(KeyCode.Space) ? 10 : 30);
        }
    }

    [RPC]
    public void RPCSetTeam(int t)
    {
        CallRPC(true, t);
        team = (Team)t;
    }
    public bool dead { get { return !enabled && car == null; } }
    public override void OnSetID()
    {
        if (isOwner)
            name = "LocalPlayer";
        else
            name = "RemotePlayer" + OwnerID;
        _Spawn.players.Add(OwnerID, this);
    }
    [RPC]
    public void RPCSelectGun(int i)
    {
        CallRPC(true, i);        
        foreach (GunBase gb in guns)
            gb.DisableGun();
        guns[i].EnableGun();

    }
    [RPC]
    private void RPCSpec()
    {
        Show(false);
    }
    public override void Dispose()
    {
        players.Remove(networkView.owner.GetHashCode());
        base.Dispose();
    }
    void OnCollisionEnter(Collision collisionInfo)
    {
        box b = collisionInfo.gameObject.GetComponent<box>();
        if (b != null && isOwner && b.OwnerID != -1 && (b.isOwner || players[b.OwnerID].team != team || dm) &&
            !(b is Player) && !(b is Zombie) &&
            collisionInfo.impactForceSum.sqrMagnitude > 150 &&
            rigidbody.velocity.magnitude < collisionInfo.rigidbody.velocity.magnitude)
        {
            RPCSetLife(-Math.Min(110, (int)collisionInfo.impactForceSum.sqrMagnitude / 2), b.OwnerID);
        }
    }
    [RPC]
    void RPCSetNick(string nick)
    {
        CallRPC(true, nick);
        Nick = nick;
    }
    const int life = 100;
    [RPC]
    public override void RPCHealth()
    {
        CallRPC(true);
        if (Life < life)
            Life += 10;
        if (freezedt > 0)
            freezedt = 0;
        guns[0].bullets += 10;
    }

    [RPC]
    public override void RPCSetLife(int NwLife, int killedby)
    {
        CallRPC(true, NwLife, killedby);
        if (isOwner)
            blood.Hit(Mathf.Abs(NwLife) * 2);

        if (isEnemy(killedby))
        {
            if (killedby == -1 || dm)
                Life += NwLife;
            else
            {
                if (zombi)
                    freezedt -= NwLife;
                else if (tdm)
                    Life += NwLife;
            }
        }
        if (Life < 0 && isOwner)
            RPCDie(killedby);

    }
    [RPC]
    public override void RPCDie(int killedby)
    {
        CallRPC(true, killedby);
        Base a = ((Transform)Instantiate(bloodexp, transform.position, Quaternion.identity)).GetComponent<Base>();
        a.Destroy(10000);
        a.transform.parent = _Spawn.effects;
        if (isOwner)
        {
            if (!zombi) _TimerA.AddMethod(10000, RPCSpawn);            
            foreach (Player p in GameObject.FindObjectsOfType(typeof(Player)))
            {
                if (p.OwnerID == killedby)
                {
                    if (p.isOwner)
                    {
                        _Loader.rpcwrite(_LocalPlayer.Nick + " died byself");
                        _LocalPlayer.RPCSetFrags(-1);
                    }
                    else if (p.team != _LocalPlayer.team || dm)
                    {
                        _Loader.rpcwrite(p.Nick + " killed " + _LocalPlayer.Nick);
                        p.RPCSetFrags(+1);
                    }
                    else
                    {
                        _Loader.rpcwrite(p.Nick + " friendly fired " + _LocalPlayer.Nick);
                        p.RPCSetFrags(-1);

                    }
                }
            }
            if (killedby == -1)
            {
                _Loader.rpcwrite(_LocalPlayer.Nick + " screwed");
                _LocalPlayer.RPCSetFrags(-1);
            }

            lockCursor = false;
        }
        Show(false);

    }

    [RPC]
    public void RPCSetFrags(int i)
    {
        CallRPC(true, i);
        frags += i;
    }
    public static Vector3 Clamp(Vector3 velocityChange, float maxVelocityChange)
    {
        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
        velocityChange.y = Mathf.Clamp(velocityChange.y, -maxVelocityChange, maxVelocityChange);
        return velocityChange;
    }
    public override Vector3 SpawnPoint()
    {
        Transform t = _Spawn.transform.Find(team.ToString());

        return t.GetChild(UnityEngine.Random.Range(0, t.childCount)).transform.position;
    }
    [RPC]
    public void RPCCarIn()
    {
        CallRPC(true);

        Show(false);
    }
}