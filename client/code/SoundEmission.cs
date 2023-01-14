using Godot;
using System;

public class SoundEmission : Node2D
{
    [Signal] delegate void WaveReceivedBy(int submarineID, float collisionAngle, long collisionTicks);

    ColorRect cone;
    PackedScene soundwavePackedScene;

    Timer emissionTimer;

    float thetaRange;
    float emissionPeriod;

    public override void _Ready()
    {
        cone = GetNode<ColorRect>("Cone");
        soundwavePackedScene = ResourceLoader.Load<PackedScene>("res://scenes/Soundwave.tscn");

        // Set up variables for soundwaves emitted
        thetaRange = 60.0f;
        emissionPeriod = 4.0f;

        emissionTimer = new Timer();
        emissionTimer.WaitTime = 0.07f*emissionPeriod;
        emissionTimer.Autostart = false;
        emissionTimer.OneShot = true;
        AddChild(emissionTimer);
    }

    public override void _Process(float delta)
    {
        //Vector2 mousePosition = GetLocalMousePosition();
        //RotationDegrees = (180.0f/Mathf.Pi)*mousePosition.Angle();
        cone.RectRotation = (GetGlobalMousePosition()-GlobalPosition).Angle()+Mathf.Pi/2;
    }

    public void EmitSoundwave(bool dot)
    {
        if (!emissionTimer.IsStopped())
            return;

        Soundwave soundwave = ResourceLoader.Load<PackedScene>("res://scenes/Soundwave.tscn").Instance<Soundwave>();
        AddChild(soundwave);

        soundwave.Rotation = cone.RectRotation;
        soundwave.Connect("WaveReceivedBy",this,"ReceiveWave");
        soundwave.PropagateWave(0.0f,1440.0f,(dot) ? 12.0f : 24.0f,thetaRange,5.0f,true);

        emissionTimer.Start();
    }

    public void ReceiveWave(int submarineID, float collisionAngle, long collisionTicks)
    {
        EmitSignal("WaveReceivedBy",submarineID,collisionAngle,collisionTicks); // Pass values up to NavigationDisplay...
    }
}
