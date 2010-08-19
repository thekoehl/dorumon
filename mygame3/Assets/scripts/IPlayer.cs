﻿using System;
using System.Collections.Generic;
using UnityEngine;
using doru;
public abstract class IPlayer : box 
{
    public Transform title;
    public Transform mesh;
    public NetworkPlayer? killedyby;
    public int Life;
    
    public bool isdead { get { return !enabled; } }
    public GunBase[] guns { get { return this.GetComponentsInChildren<GunBase>(); } }
    internal Team? team
    {
        get
        {
            if (OwnerID == null) return null;
            else return _Spawn.players[OwnerID.Value].team;
        }
    }
    
    public override void Dispose()
    {
        _Spawn.iplayers.Remove(this);
    }
    protected override void Start()
    {
        _Spawn.iplayers.Add(this);
        base.Start();
    }


    protected override void Update()
    {
        if (mesh != null)
        {
            if (OwnerID != null)
                mesh.renderer.material.color = team == Team.ata ? Color.red : Color.blue;
            else
                mesh.renderer.material.color = Color.white;
        }
        if (title != null)
        {
            if (OwnerID != null && !isOwner && _LocalPlayer != null && _LocalPlayer.team == team)
                title.GetComponent<TextMesh>().text = _Spawn.players[OwnerID.Value].Nick;
            else
                title.GetComponent<TextMesh>().text = "";
        }
        base.Update();
        
    }
    [RPC]
    public virtual void RPCSetLife(int NwLife)
    {
        CallRPC(true, NwLife);
        if(killedyby == null || _Spawn.players[killedyby.Value].team != team)
            Life = NwLife;

        if (NwLife < 0)
            RPCDie();
    }
    
    [RPC]
    public abstract void RPCDie();


    
}
