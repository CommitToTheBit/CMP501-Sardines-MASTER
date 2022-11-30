using Godot;
using System;

public class Main : Control
{
    public Control textControl;
    public Text text;

    public override void _Ready()
    {
        textControl = GetNode<Control>("TextControl");
        text = textControl.GetChild<Text>(0);
        text.Connect("ChangeUI",this,"ChangeUI");
        text.Fade(true);
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionPressed("ui_cancel"))
        {
            if (!(text is MainMenuText))
            {
                ChangeUI("MainMenu","MainMenu");
            }
            else
            {
                GetTree().Quit();
            }

        }

    }

    public async void ChangeUI(string textID, string displayID)
    {
        // STEP 1: Fade current text out...
        textControl.GetChild<Text>(0).Fade(false);

        // STEP 2: Switch text while hidden...
        text.Disconnect("ChangeUI",this,"ChangeUI");
        textControl.GetChild(0).QueueFree();
        await ToSignal(GetTree(),"idle_frame");

        textControl.AddChild(ResourceLoader.Load<PackedScene>("res://scenes/"+textID+"Text.tscn").Instance());
        text = textControl.GetChild<Text>(0);
        text.Connect("ChangeUI",this,"ChangeUI");

        // STEP 3: Fade new text in...
        textControl.GetChild<Text>(0).Fade(true);
    }
}
