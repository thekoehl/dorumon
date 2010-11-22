﻿using System;
using UnityEngine;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
public class Console : Base
{
    public static StringBuilder log = new StringBuilder();
    Rect r;
    protected override void Awake()
    {
        base.Awake();
        Application.RegisterLogCallback(onLog);
    }
    public int errorcount;
    void onLog(string condition, string stackTrace, LogType type)
    {
        try
        {
            
            if (type == LogType.Exception || type == LogType.Error) errorcount++;
            log.AppendLine(Regex.Match(stackTrace, @"^\w+\:\w+", RegexOptions.Multiline).Value + ": " + condition);
            
        }
        catch { }
    }

    public void OnGUI()
    {
        GUI.skin = _Loader._skin;
        r = new Rect(0, 0, Screen.width, Screen.height);
        GUI.Window(-1, r, Window, "Console");

    }
    
    void Window(int id)
    {        
        GUI.Box(r, "");        
        GUI.TextField(r, log.ToString(), GUI.skin.customStyles[8]);
        GUILayout.Label("Error Count:" + errorcount);
        GUI.BringWindowToFront(-1);
        GUI.FocusWindow(-1);
    }
    
    //}


}