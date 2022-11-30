using Godot;
using System;

public class Main : Control
{
    public Control textControl;
    public Text text;

    // Recordings vars
    private AudioEffectRecord _effect;
    private AudioStreamSample _recording;

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
        _effect = (AudioEffectRecord)AudioServer.GetBusEffect(idx, 0);
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

        // Recording inputs
        if (Input.IsActionPressed("ui_talk"))
        {
            if (_effect.IsRecordingActive())
            {
                _recording = _effect.GetRecording();
                //GetNode<Button>("PlayButton").Disabled = false;
                //GetNode<Button>("SaveButton").Disabled = false;
                _effect.SetRecordingActive(false);
                //GetNode<Button>("RecordButton").Text = "Record";
                //GetNode<Label>("Status").Text = "";
                GD.Print("Stopping...");
            }
            else
            {
                //GetNode<Button>("PlayButton").Disabled = true;
                //GetNode<Button>("SaveButton").Disabled = true;
                _effect.SetRecordingActive(true);
                //GetNode<Button>("RecordButton").Text = "Stop";
                //GetNode<Label>("Status").Text = "Recording...";
                GD.Print("Recording...");
            }
        }
        else if (Input.IsActionPressed("ui_playback"))
        {
            _recording.SaveToWav("res://temp.wav");
            //GetNode<Label>("Status").Text = string.Format("Saved WAV file to: {0}\n({1})", savePath, ProjectSettings.GlobalizePath(savePath));
            GD.Print("Saved!");

            GD.Print(_recording);
            GD.Print(_recording.Format);
            GD.Print(_recording.MixRate);
            GD.Print(_recording.Stereo);
            byte[] data = _recording.Data;
            GD.Print(data.Length);
            var audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamRecord");
            audioStreamPlayer.Stream = _recording;
            audioStreamPlayer.Play();
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
