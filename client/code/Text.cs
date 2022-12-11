using Godot;
using System;
using System.Collections.Generic;

public class Text : VBoxContainer
{
    [Signal] delegate void ChangeUI(string textID, string displayID, List<string> textHistory);

    Tween tween;

    protected string id;
    public List<string> history;

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionPressed("ui_cancel"))
        {
            List<string> newHistory = new List<string>(history);
            newHistory.Add(id);

            int resumeIndex = newHistory.FindIndex(x => x.Equals("Pause"))-1;

            if (resumeIndex >= 0)
            {
                for (int i = newHistory.Count-1; i >= resumeIndex; i--)
                    newHistory.RemoveAt(i);

                EmitSignal("ChangeUI",history[resumeIndex],history[resumeIndex],newHistory);
            }
            else if (!(id == "MainMenu") && !(id == "Settings") && !(id == "JoinGame")) // FIXME: For LobbyText, need to edit Desert to something less... severe... not "&& !(id == "Lobby")"!
            {
                EmitSignal("ChangeUI","Pause","Pause",newHistory);
            }
            else if (!(id == "MainMenu"))
            {
                EmitSignal("ChangeUI","MainMenu","MainMenu",new List<string>());
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
            else if (child is PseudoButton)
            {
                child.Connect("focus_entered",this,"PseudoButtonFocused",new Godot.Collections.Array() {child,true});
                child.Connect("focus_exited",this,"PseudoButtonFocused",new Godot.Collections.Array() {child,false});
                child.Connect("mouse_entered",this,"PseudoButtonHovered",new Godot.Collections.Array() {child});
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

    public void PseudoButtonFocused(PseudoButton pseudoButton, bool focus)
    {
        pseudoButton.SetBbcode(focus);
    }

    public void PseudoButtonHovered(PseudoButton pseudoButton)
    {
        pseudoButton.GrabFocus();
    }
}
