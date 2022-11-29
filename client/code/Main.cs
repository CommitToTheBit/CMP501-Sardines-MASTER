using Godot;
using System;

public class Main : Control
{
    public Control textControl;
    public Text text;


    public override void _Ready()
    {
        textControl = GetNode<Control>("TextControl");
        text = textControl.GetNode<Text>("MainMenuText");
        text.Connect("ChangeScene",this,"ChangeScene");
        text.Fade(true);
    }

    public void ChangeScene(string textID, string displayID)
    {
        // STEP 1: Fade text out
        text.Fade(false);

        // STEP 2
    }
}
