using Godot;
using System;
using System.Collections.Generic;

public class LobbyText : Text
{
    public override void _Ready()
    {
        InitialiseText();
        GetNode<TextureButton>("StartGameButton").GrabFocus();
    }

    public void StartGamePressed()
    {
        List<string> newHistory = new List<string>(history);
        newHistory.Add(id);

        EmitSignal("ChangeUI","Navigation","Navigation",newHistory); // FIXME: Cut to a game loading screen, then decide on Navigation/else once role received
    }

    public void BackPressed()
    {
        string backID = history[history.Count-1];
        List<string> newHistory = new List<string>(history);
        newHistory.RemoveAt(history.Count-1);

        EmitSignal("ChangeUI",backID,backID,newHistory);
    }
}
