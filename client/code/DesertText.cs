using Godot;
using System;
using System.Collections.Generic;

public class DesertText : Text
{
    private Handler handler;

    public override void _Ready()
    {
        handler = GetNode<Handler>("/root/Handler");

        InitialiseText();
        GetNode<TextureButton>("NegativeButton").GrabFocus(); 
    }

    public void AffirmativePressed()
    {
        handler.client.Disconnect();

        // Reset client's networking component (except for clientID!)
        int clientID = handler.client.GetClientID();
        handler.client = new Client();
        handler.client.SetClientID(clientID);

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
