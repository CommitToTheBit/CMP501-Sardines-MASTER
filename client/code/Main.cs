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

    public void ChangeUI(string textID, string displayID)
    {
        // STEP 1: Fade text out
        text.Fade(false);

        // STEP 2: Switch scene
        textControl.GetChild(0).QueueFree();
        //text.CallDeferred("free");

        GD.Print(textID);

        //text = ;
        textControl.AddChild(ResourceLoader.Load<PackedScene>("res://scenes/"+textID+"Text.tscn").Instance());
        text = textControl.GetChild<Text>(0);

        // STEP 3: Fade text in
        text.Fade(true);
    }
}
