using Godot;
using System;
using System.Collections.Generic;

public class Text : VBoxContainer
{
    [Signal]
    delegate void ChangeUI(string textID, string displayID, List<string> textHistory);

    Tween tween;

    protected string id;
    public List<string> history;

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionPressed("ui_cancel"))
        {
            // If paused, return from the pause menu
            int resumeIndex = history.FindIndex(x => x.Equals("Pause"))-1;

            if (resumeIndex >= 0)
            {
                List<string> newHistory = new List<string>();
                for (int i = 0; i < resumeIndex; i++)
                    newHistory.Add(history[i]);

                EmitSignal("ChangeUI",history[resumeIndex],history[resumeIndex],newHistory);

                // DEBUG:
                GD.Print()
            }
            else if (!(this is MainMenuText))
            {
                List<string> newHistory = new List<string>(history);
                newHistory.Add(id);

                EmitSignal("ChangeUI","Pause","Pause",newHistory);
            }
            else
            {
                GetTree().Quit();
            }

        }
    }

    public void InitialiseText()
    {
        id = Name.Substring(0,Name.Length-4);

        foreach (Node child in GetChildren())
        {
            if (child is TextureButton)
            {
                string buttonID = child.Name.Substring(0,child.Name.Length-6);
                child.Connect("focus_entered",this,"ButtonFocused",new Godot.Collections.Array() {child,"res://assets/art/text buttons/"+id+buttonID+"Hover.png"});
                child.Connect("focus_exited",this,"ButtonFocused",new Godot.Collections.Array() {child,"res://assets/art/text buttons/"+id+buttonID+"Normal.png"});
                child.Connect("mouse_entered",this,"ButtonHovered",new Godot.Collections.Array() {child});
                child.Connect("pressed",this,buttonID+"Pressed");
            }

            Hide();
        }

        tween = new Tween();
    }

    public void Fade(bool fadeIn)
    {
        if (fadeIn)
        {
            foreach (Node child in GetChildren())
                Show();
        }
        else
        {
            foreach (Node child in GetChildren())
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
