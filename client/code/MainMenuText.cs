using Godot;
using System;
using System.Collections.Generic;

public class MainMenuText : Text
{
    private Handler handler;

    public override void _Ready()
    {
        handler = GetNode<Handler>("/root/Handler");

        InitialiseText();
        GetNode<TextureButton>("JoinGameButton").GrabFocus(); 
    }

    public void JoinGamePressed()
    {
        List<string> newHistory = new List<string>() {"MainMenu"};

        handler.c.Connect();

        if (handler.c.IsConnected())
            EmitSignal("ChangeUI","Lobby","Lobby",newHistory);
        else
            // DEBUG:
            GD.Print("Cannot connect!");
    }

    public void HostGamePressed()
    {
        List<string> newHistory = new List<string>() {"MainMenu"};

        EmitSignal("ChangeUI","Lobby","Lobby",newHistory);
    }

    public void SettingsPressed()
    {
        List<string> newHistory = new List<string>() {"MainMenu"};

        EmitSignal("ChangeUI","Settings","Settings",newHistory);
    }

    public void QuitPressed()
    {
        GetTree().Quit();
    }
}
