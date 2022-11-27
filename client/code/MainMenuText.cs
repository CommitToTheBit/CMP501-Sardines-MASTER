using Godot;
using System;

public class MainMenuText : VBoxContainer
{
    [Signal]
    delegate void ChangeScene(string textID, string displayID);

    TextureButton joinGame;
    TextureButton hostGame;
    TextureButton settings;
    TextureButton quit;

    Tween tween;

    public override void _Ready()
    {
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

        tween = GetNode<Tween>("Tween");        
    }

    public void ButtonFocused(TextureButton button, string path)
    {
        button.TextureNormal = GD.Load<Texture>(path);
    }

    public void ButtonHovered(TextureButton button)
    {
        button.GrabFocus();
    }

    public void JoinGamePressed()
    {
        EmitSignal("Lobby","Lobby");
    }

    public void HostGamePressed()
    {

    }

    public void SettingsPressed()
    {

    }

    public void QuitPressed()
    {
        GetTree().Quit();
    }
}
