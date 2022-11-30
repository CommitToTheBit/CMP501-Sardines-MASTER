using Godot;
using System;

public class LobbyText : Text
{
    TextureButton startGame;
    TextureButton back;

    public override void _Ready()
    {
        startGame = GetNode<TextureButton>("StartGameButton");
        startGame.Connect("focus_entered",this,"ButtonFocused",new Godot.Collections.Array() {startGame,"res://assets/Start Game Button (Lobby, Hover).png"});
        startGame.Connect("focus_exited",this,"ButtonFocused",new Godot.Collections.Array() {startGame,"res://assets/Start Game Button (Lobby, Normal).png"});
        startGame.Connect("mouse_entered",this,"ButtonHovered",new Godot.Collections.Array() {startGame});
        startGame.Connect("pressed",this,"StartGamePressed");
        startGame.GrabFocus();

        back = GetNode<TextureButton>("BackButton");
        back.Connect("focus_entered",this,"ButtonFocused",new Godot.Collections.Array() {back,"res://assets/Back Button (Lobby, Hover).png"});
        back.Connect("focus_exited",this,"ButtonFocused",new Godot.Collections.Array() {back,"res://assets/Back Button (Lobby, Normal).png"});
        back.Connect("mouse_entered",this,"ButtonHovered",new Godot.Collections.Array() {back});
        back.Connect("pressed",this,"BackPressed"); 
    }

    public void StartGamePressed()
    {

    }

    public void BackPressed()
    {
        EmitSignal("ChangeUI","MainMenu","MainMenu");
    }
}
