using Godot;
using System;

public class Text : VBoxContainer
{
    [Signal]
    delegate void ChangeScene(string textID, string displayID);

    public override void _Ready()
    {
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
}
