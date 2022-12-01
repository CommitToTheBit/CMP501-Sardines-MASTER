using Godot;
using System;
using System.Collections.Generic;

public class DesertText : Text
{
    public override void _Ready()
    {
        InitialiseText();
        GetNode<TextureButton>("NegativeButton").GrabFocus(); 
    }

    public void AffirmativePressed()
    {
        EmitSignal("ChangeUI","MainMenu","MainMenu",new List<string>());
    }

    public void NegativePressed()
    {
        string backID = history[history.Count-1];
        List<string> newHistory = new List<string>(history);
        newHistory.RemoveAt(history.Count-1);

        EmitSignal("ChangeUI",backID,backID,newHistory);
    }
}
