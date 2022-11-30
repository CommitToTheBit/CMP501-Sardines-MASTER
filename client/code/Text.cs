using Godot;
using System;

public class Text : VBoxContainer
{
    [Signal]
    delegate void ChangeUI(string textID, string displayID);

    Tween tween;

    public override void _Ready()
    {
        tween = new Tween();

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
}
