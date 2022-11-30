using Godot;
using System;
using System.Collections.Generic;

public class PauseText : Text
{
    public override void _Ready()
    {
        InitialiseText();
        GetNode<TextureButton>("ResumeButton").GrabFocus();
    }

    public void ResumePressed()
    {
        string resumeID = history[history.Count-1];
        List<string> newHistory = new List<string>(history);
        newHistory.RemoveAt(history.Count-1);

        EmitSignal("ChangeUI",resumeID,resumeID,newHistory);
    }

    public void SettingsPressed()
    {
        List<string> newHistory = new List<string>(history);
        newHistory.Add(id);

        EmitSignal("ChangeUI","Settings","Settings",newHistory);
    }

    public void DesertPressed()
    {

    }
}
