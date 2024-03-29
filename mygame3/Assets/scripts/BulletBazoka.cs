﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class BulletBazoka : BulletBase
{
    
    public Transform exp;
    
    
    public float velocity;

    protected override void FixedUpdate()
    {

        this.transform.position += transform.TransformDirection(Vector3.forward) * velocity * Time.deltaTime;
        base.FixedUpdate();
    }


    protected override void Hit(RaycastHit hit)
    {
        
        base.Hit(hit);
        Vector3 vector3 = hit.point - this.transform.rotation * new Vector3(0, 0, 1);
        Transform o;
        Destroy(o = (Transform)Instantiate(exp, vector3, Quaternion.identity), 10);
        o.GetComponent<Explosion>().OwnerID = OwnerID;
        
        if (Root(hit.collider.gameObject).tag == "Level" && decal != null)
        {
            
            Transform a;
            Destroy((a = (Transform)Instantiate(decal, hit.point, Quaternion.LookRotation(hit.normal))), 10);
            a.parent = _Spawn.effects;
        }

        Destroy(gameObject);        
    }

}
