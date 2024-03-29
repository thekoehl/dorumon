﻿using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Object = UnityEngine.Object;
using System.Collections;
using System.Text.RegularExpressions;

#if (UNITY_EDITOR)
using UnityEditor;
#endif
public class Base : MonoBehaviour
{
    public virtual void Awake()
    {
       
    }
    public static Vector3 ZeroY(Vector3 v)
    {
        v.y = 0;
        return v;
    }
    public static Vector3 ZeroYNorm(Vector3 v)
    {
        v.y = 0;
        return v.normalized;
    }
    public float clamp(float angle)
    {
        if (angle > 180) angle -= 360f;
        return angle;
    }
    public void Active(bool value)
    {
        foreach (Transform a in this.GetComponentsInChildren<Transform>())
            a.gameObject.active = false;
        this.gameObject.active = false;
    }
    public bool isEditor
    {
        get
        {
            return
            Application.isEditor;
        }
    }
    public Transform tr { get { return transform; } }
    public Vector3 pos { get { return tr.position; } set { tr.position = value; } }
    public Vector3 position { get { return tr.position; } set { tr.position = value; } }
    public Transform parent { get { return tr.parent; } set { tr.parent = value; } }
    public Vector3 scale { get { return tr.localScale; } set { tr.localScale = value; } }
    public float posx { get { return pos.x; } set { var v = pos; v.x = value; pos = v; } }
    public float posy { get { return pos.y; } set { var v = pos; v.y = value; pos = v; } }
    public float posz { get { return pos.z; } set { var v = pos; v.z = value; pos = v; } }
    public float rotx { get { return rot.eulerAngles.x; } set { var e = rot.eulerAngles; e.x = value; rot = Quaternion.Euler(e); } }
    public float roty { get { return rot.eulerAngles.y; } set { var e = rot.eulerAngles; e.y = value; rot = Quaternion.Euler(e); } }
    public float rotz { get { return rot.eulerAngles.z; } set { var e = rot.eulerAngles; e.z = value; rot = Quaternion.Euler(e); } }
    public Quaternion rot { get { return tr.rotation; } set { tr.rotation = value; } }
    public Vector3 rote { get { return tr.rotation.eulerAngles; } set { tr.rotation = Quaternion.Euler(value); } }

    public bool selected
    {
        get
        {
#if (UNITY_EDITOR)
            return UnityEditor.Selection.activeGameObject == this.gameObject;
#else
            return false;
#endif
        }
    }
    public static void IgnoreAll(string name, params string[] layers)
    {
        for (int i = 0; i < 31; i++)
            if (!layers.Contains(LayerMask.LayerToName(i)))
                Physics.IgnoreLayerCollision(LayerMask.NameToLayer(name), i, true);
    }


    public virtual void OnSelectionChanged()
    {
    }

    public virtual void Init()
    {
    }
    public virtual void OnEditorGui()
    {

    }

    public static void Combine(GameObject g)
    {
        Component[] filters = g.GetComponentsInChildren(typeof(MeshFilter));
        Matrix4x4 myTransform = g.transform.worldToLocalMatrix;
        Hashtable materialToMesh = new Hashtable();

        foreach (Component t in filters)
        {
            MeshFilter filter = (MeshFilter)t;
            Renderer curRenderer = t.renderer;
            MeshCombineUtility.MeshInstance instance = new MeshCombineUtility.MeshInstance();
            instance.mesh = filter.sharedMesh;
            if (curRenderer != null && curRenderer.enabled && instance.mesh != null)
            {
                instance.transform = myTransform * filter.transform.localToWorldMatrix;

                Material[] materials = curRenderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    instance.subMeshIndex = System.Math.Min(m, instance.mesh.subMeshCount - 1);

                    ArrayList objects = (ArrayList)materialToMesh[materials[m]];
                    if (objects != null)
                    {
                        objects.Add(instance);
                    }
                    else
                    {
                        objects = new ArrayList { instance };
                        materialToMesh.Add(materials[m], objects);
                    }
                }

                curRenderer.enabled = false;
            }
        }

        foreach (DictionaryEntry de in materialToMesh)
        {
            ArrayList elements = (ArrayList)de.Value;
            MeshCombineUtility.MeshInstance[] instances = (MeshCombineUtility.MeshInstance[])elements.ToArray(typeof(MeshCombineUtility.MeshInstance));

            // We have a maximum of one material, so just attach the mesh to our own game object
            if (materialToMesh.Count == 1)
            {
                // Make sure we have a mesh filter & renderer
                if (g.GetComponent(typeof(MeshFilter)) == null)
                    g.gameObject.AddComponent(typeof(MeshFilter));
                if (!g.GetComponent("MeshRenderer"))
                    g.gameObject.AddComponent("MeshRenderer");

                MeshFilter filter = (MeshFilter)g.GetComponent(typeof(MeshFilter));
                filter.mesh = MeshCombineUtility.Combine(instances, true);
                g.renderer.material = (Material)de.Key;
                g.renderer.enabled = true;
            }
            // We have multiple materials to take care of, build one mesh / gameobject for each material
            // and parent it to this object
            else
            {
                GameObject go = new GameObject("Combined mesh");
                go.transform.parent = g.transform;
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localPosition = Vector3.zero;
                go.AddComponent(typeof(MeshFilter));
                go.AddComponent("MeshRenderer");
                go.renderer.material = (Material)de.Key;
                MeshFilter filter = (MeshFilter)go.GetComponent(typeof(MeshFilter));
                filter.mesh = MeshCombineUtility.Combine(instances, true);
            }
        }
    }
#if (UNITY_EDITOR)
    public virtual void OnSceneGUI(SceneView scene, ref bool repaint)
    {

    }
#endif

}