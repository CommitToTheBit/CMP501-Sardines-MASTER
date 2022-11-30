using Godot;
using System;

public class Main : Control
{
    public Control textControl;
    public Text text;

    // Recordings vars
    private AudioEffectCapture _effect;
    private AudioStreamSample _recording;

    int frameCount;

    public override void _Ready()
    {
        textControl = GetNode<Control>("TextControl");
        text = textControl.GetChild<Text>(0);
        text.Connect("ChangeUI",this,"ChangeUI");
        text.Fade(true);

        // Recording set-up
        // We get the index of the "Record" bus.
        int idx = AudioServer.GetBusIndex("Record");
        // And use it to retrieve its first effect, which has been defined
        // as an "AudioEffectRecord" resource.
        _effect = (AudioEffectCapture)AudioServer.GetBusEffect(idx, 0);
        _effect.BufferLength = 0.1f;
        frameCount = 0;
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

    public override void _Process(float delta)
    {
        AudioStreamPlayer audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        AudioStreamGenerator generator = new AudioStreamGenerator();

        Godot.Vector2[] data = _effect.GetBuffer(_effect.GetBufferLengthFrames());
        _effect.ClearBuffer();
        
        foreach (Vector2 frame in data)
        {
            generator.
        }

        audioStreamPlayer.Play();
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
