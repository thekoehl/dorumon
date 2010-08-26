﻿using System;
using System.Collections.Generic;

using UnityEngine;

public class GunPhysix : GunBase
{
    public float gravitaty = 1;
    public float radius = 50;
    public float exp = 2000;
    public float expradius = 40;
    public float scalefactor = 10;
    public bool power;
    
    
    protected override void FixedUpdate()
    {
        
        
        if (power)
        {
            
            if (bullets < exp) bullets+=80;
            foreach (Base b in _Spawn.dynamic)
            {
                if (b is bloodexp || b.GetType() == typeof(box))
                {
                    b.rigidbody.AddExplosionForce(-gravitaty*scalefactor, cursor.position, radius);
                    b.rigidbody.velocity *= .97f;
                    b.OwnerID = Root(this.gameObject).GetComponent<Player>().OwnerID;
                }

            }
        }
        base.FixedUpdate();

    }

    [RPC]
    public void RPCSetPower(bool enable)
    {
        CallRPC(true, enable);
        power = enable;
        if (!enable)
        {
            this.GetComponents<AudioSource>()[0].Stop();
            if (bullets > 300)
                this.GetComponents<AudioSource>()[1].Play();
            foreach (Base b in _Spawn.dynamic)
                if ((b is bloodexp || typeof(box) == b.GetType())
                    && Vector3.Distance(b.transform.position, cursor.position) < expradius)
                    b.rigidbody.AddForce(this.transform.rotation * new Vector3(0, 0, bullets*scalefactor));
            bullets = 0;

        }
        else
            this.GetComponents<AudioSource>()[0].Play();
    }

    
    protected override void LocalUpdate()
    {
        if (isOwner && enabled)
        {
            if (Input.GetMouseButtonDown(0))
                RPCSetPower(true);
            else if (Input.GetMouseButtonUp(0))
                RPCSetPower(false);
        }
        
    }

    

}
