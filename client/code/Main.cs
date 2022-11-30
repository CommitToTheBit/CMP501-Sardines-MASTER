using Godot;
using System;
using System.Collections.Generic;

public class Main : Control
{
    public Control textControl;
    public Text text;

    // Recordings vars
    private AudioEffectCapture _effect;

    public override void _Ready()
    {
        textControl = GetNode<Control>("TextControl");
        text = textControl.GetChild<Text>(0);
        text.Connect("ChangeUI",this,"ChangeUI");
        text.Fade(true);

        // AudioStreamGenerator testing...
        AudioStreamPlayer audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        AudioStreamGeneratorPlayback playback = audioStreamPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;

        _effect = (AudioEffectCapture)AudioServer.GetBusEffect(1, 1);
        _effect.BufferLength = 1.0f;

        for (int i = 0; i < playback.GetFramesAvailable(); i++)
        {
            //playback.PushFrame(Vector2.Zero);
        }

        audioStreamPlayer.Play();
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionPressed("ui_cancel"))
        {
            if (text is PauseText)
            {
                //ChangeUI("MainMenu","MainMenu",new List<string>());
            }
            else if (!(text is MainMenuText))
            {
                //ChangeUI("Pause","Pause");
            }
            else
            {
                GetTree().Quit();
            }

        }
    }

    public override void _Process(float delta)
    {
        // AudioStreamGenerator testing...
        AudioStreamPlayer audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        AudioStreamGeneratorPlayback playback = audioStreamPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;

        Vector2[] frames = _effect.GetBuffer(_effect.GetFramesAvailable());
        //_effect.ClearBuffer();

        var to_fill = playback.GetFramesAvailable();
        for (int i = 0; i < playback.GetFramesAvailable(); i++)
        {
            try
            {
                playback.PushFrame(frames[i]);
            }
            catch 
            {
                //playback.PushFrame(Vector2.Zero);
            }
        }

        audioStreamPlayer.Play();
    }

    public async void ChangeUI(string textID, string displayID, List<string> init_history)
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

        // FIXME: Consider the protection level here...
        text.history = init_history;

        // STEP 3: Fade new text in...
        textControl.GetChild<Text>(0).Fade(true);
    }
}
