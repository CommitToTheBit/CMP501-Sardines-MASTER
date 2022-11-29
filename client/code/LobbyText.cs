using Godot;
using System;

public class LobbyText : VBoxContainer
{
    [Signal]
    delegate void ChangeScene(string textID, string displayID);

    TextureButton startGame;

    Tween tween;

    public override void _Ready()
    {
        startGame = GetNode<TextureButton>("StartGameButton");
        startGame.Connect("focus_entered",this,"ButtonFocused",new Godot.Collections.Array() {startGame,"res://assets/Start Game Button (Lobby, Hover).png"});
        startGame.Connect("focus_exited",this,"ButtonFocused",new Godot.Collections.Array() {startGame,"res://assets/Start Game Button (Lobby, Normal).png"});
        startGame.Connect("mouse_entered",this,"ButtonHovered",new Godot.Collections.Array() {startGame});
        startGame.Connect("pressed",this,"StartGamePressed");
        startGame.GrabFocus();

        tween = GetNode<Tween>("Tween");  

        // Hide everything...
        Fade(false);        
    }

    public void Fade(bool fadeIn)
    {
        if (fadeIn)
        {
            foreach (var child in GetChildren())
                Show();
        }
        else
        {
            foreach (var child in GetChildren())
                Hide();
        }
    }

    public void ButtonFocused(TextureButton button, string path)
    {
        button.TextureNormal = GD.Load<Texture>(path);
    }

    public void ButtonHovered(TextureButton button)
    {
        button.GrabFocus();
    }

    public void StartGamePressed()
    {

    }
}
