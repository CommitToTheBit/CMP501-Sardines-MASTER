using Godot;
using System;
using System.Collections.Generic;

public class LobbyText : Text
{
    Handler handler;

    public override void _Ready()
    {
        handler = GetNode<Handler>("/root/Handler");
        handler.client.Connect("ReceivedPacket",this,"Receive");

        InitialiseText();
        GetNode<TextureButton>("StartGameButton").GrabFocus();
    }

    public void StartGamePressed()
    {
        handler.client.Send2310();
    }

    public void BackPressed()
    {
        string backID = history[history.Count-1];
        List<string> newHistory = new List<string>(history);
        newHistory.RemoveAt(history.Count-1);

        EmitSignal("ChangeUI",backID,backID,newHistory);
    }

    public void Receive(int packetID)
    {
        switch (packetID)
        {
            case 2311:
                List<string> newHistory = new List<string>(history);
                newHistory.Add(id);

                // FIXME: Run for loop/function to find client role (from handler.client.state)
                EmitSignal("ChangeUI","Navigation","Navigation",newHistory);
                return;
        }
    }
}
