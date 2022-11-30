using Godot;
using System;

public class SettingsText : Text
{
    TextureButton back;

    public override void _Ready()
    {
        back = GetNode<TextureButton>("BackButton");
        back.Connect("focus_entered",this,"ButtonFocused",new Godot.Collections.Array() {back,"res://assets/Back Button (Settings, Hover).png"});
        back.Connect("focus_exited",this,"ButtonFocused",new Godot.Collections.Array() {back,"res://assets/Back Button (Settings, Normal).png"});
        back.Connect("mouse_entered",this,"ButtonHovered",new Godot.Collections.Array() {back});
        back.Connect("pressed",this,"BackPressed"); 
        back.GrabFocus();
    }

    public void BackPressed()
    {
        EmitSignal("ChangeUI","MainMenu","MainMenu");
    }
}
