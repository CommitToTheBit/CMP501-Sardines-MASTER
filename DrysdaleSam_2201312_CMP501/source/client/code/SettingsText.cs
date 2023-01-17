using Godot;
using System;
using System.Collections.Generic;

public class SettingsText : Text
{
    public override void _Ready()
    {
        InitialiseText();
        GetNode<TextureButton>("BackButton").GrabFocus();
    }

    public void BackPressed()
    {
        string backID = history[history.Count-1];
        List<string> newHistory = new List<string>(history);
        newHistory.RemoveAt(history.Count-1);

        EmitSignal("ChangeUI",backID,backID,newHistory);
    }
}
