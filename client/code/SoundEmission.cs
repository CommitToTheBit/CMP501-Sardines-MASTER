using Godot;
using System;

public class SoundEmission : Node2D
{
    Timer emissionTimer;
    PackedScene soundwavePackedScene;

    public override void _Ready()
    {
        soundwavePackedScene = ResourceLoader.Load<PackedScene>("res://scenes/Soundwave.tscn");

        // Set up delay between soundwaves emitted...
        emissionTimer = new Timer();
        emissionTimer.WaitTime = 0.5f;
        emissionTimer.Autostart = false;
        emissionTimer.OneShot = true;
        AddChild(emissionTimer);
    }

    public override void _Process(float delta)
    {
        //Vector2 mousePosition = GetLocalMousePosition();
        //RotationDegrees = (180.0f/Mathf.Pi)*mousePosition.Angle();
        RotationDegrees = (180.0f/Mathf.Pi)*((GetGlobalMousePosition()-Position).Angle()+Mathf.Pi/2);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (emissionTimer.IsStopped())
        {
            if (Input.IsActionPressed("ui_dot"))
            {
                //Soundwave soundwave = ResourceLoader.Load<PackedScene>("res://scenes/Soundwave.tscn").Instance<Soundwave>();
                AddChild(ResourceLoader.Load<PackedScene>("res://scenes/Soundwave.tscn").Instance<Soundwave>());
                ((Soundwave)GetChild(GetChildCount()-1)).PropagateWave(0.0f,1440.0f,8.0f,45.0f+5.0f,20.0f,false);

                AddChild(ResourceLoader.Load<PackedScene>("res://scenes/Soundwave.tscn").Instance<Soundwave>());
                ((Soundwave)GetChild(GetChildCount()-1)).PropagateWave(0.0f,1440.0f,8.0f,45.0f+5.0f,2.0f,false);
            }
        }
    }
}
