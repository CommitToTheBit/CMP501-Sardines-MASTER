using Godot;
using System;

public class MainMenuText : Text
{
    private Handler handler;

    TextureButton joinGame;
    TextureButton hostGame;
    TextureButton settings;
    TextureButton quit;

    public override void _Ready()
    {
        handler = GetNode<Handler>("/root/Handler");

        joinGame = GetNode<TextureButton>("JoinGameButton");
        joinGame.Connect("focus_entered",this,"ButtonFocused",new Godot.Collections.Array() {joinGame,"res://assets/Join Game Button (Main Menu, Hover).png"});
        joinGame.Connect("focus_exited",this,"ButtonFocused",new Godot.Collections.Array() {joinGame,"res://assets/Join Game Button (Main Menu, Normal).png"});
        joinGame.Connect("mouse_entered",this,"ButtonHovered",new Godot.Collections.Array() {joinGame});
        joinGame.Connect("pressed",this,"JoinGamePressed");
        joinGame.GrabFocus();
        
        hostGame = GetNode<TextureButton>("HostGameButton");
        hostGame.Connect("focus_entered",this,"ButtonFocused",new Godot.Collections.Array() {hostGame,"res://assets/Host Game Button (Main Menu, Hover).png"});
        hostGame.Connect("focus_exited",this,"ButtonFocused",new Godot.Collections.Array() {hostGame,"res://assets/Host Game Button (Main Menu, Normal).png"});
        hostGame.Connect("mouse_entered",this,"ButtonHovered",new Godot.Collections.Array() {hostGame});
        hostGame.Connect("pressed",this,"HostGamePressed");

        settings = GetNode<TextureButton>("SettingsButton");
        settings.Connect("focus_entered",this,"ButtonFocused",new Godot.Collections.Array() {settings,"res://assets/Settings Button (Main Menu, Hover).png"});
        settings.Connect("focus_exited",this,"ButtonFocused",new Godot.Collections.Array() {settings,"res://assets/Settings Button (Main Menu, Normal).png"});
        settings.Connect("mouse_entered",this,"ButtonHovered",new Godot.Collections.Array() {settings});
        settings.Connect("pressed",this,"SettingsPressed");

        quit = GetNode<TextureButton>("QuitButton");
        quit.Connect("focus_entered",this,"ButtonFocused",new Godot.Collections.Array() {quit,"res://assets/Quit Button (Main Menu, Hover).png"});
        quit.Connect("focus_exited",this,"ButtonFocused",new Godot.Collections.Array() {quit,"res://assets/Quit Button (Main Menu, Normal).png"});
        quit.Connect("mouse_entered",this,"ButtonHovered",new Godot.Collections.Array() {quit});
        quit.Connect("pressed",this,"QuitPressed");    
    }

    public void JoinGamePressed()
    {
        EmitSignal("ChangeUI","Lobby","Lobby");

        /*handler.c.Connect();

        if (handler.c.IsConnected())
            EmitSignal("ChangeUI","Lobby","Lobby");
        else
            // DEBUG:
            GD.Print("Cannot connect!");*/
    }

    public void HostGamePressed()
    {
        EmitSignal("ChangeUI","Lobby","Lobby");
    }

    public void SettingsPressed()
    {
        EmitSignal("ChangeUI","Settings","Settings");
    }

    public void QuitPressed()
    {
        GetTree().Quit();
    }
}
