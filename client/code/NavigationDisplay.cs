using Godot;
using System;
using System.Collections.Generic;

public class NavigationDisplay : Control
{
    private Handler handler;
    private Timer positionTimer;

    // Submarine class control:
    float thrust;
    float steer;


    // FIXME: Simple sprite set-up
    Node2D foreground;
    Node2D midground;
    Dictionary<int,Vessel> vessels;

    Vessel vessel;
    SoundEmission soundEmission;

    Sprite sweep;

    // FIXME: AudioFrames handling
    private List<Vector2> frames;
    private AudioEffectCapture _effect;

    public override void _Ready()
    {
        handler = GetNode<Handler>("/root/Handler");
        //handler.client.Connect("ReceivedFrame",this,"ReceiveFrame");

        // Set up timer for sending position packets
        positionTimer = new Timer();
        positionTimer.WaitTime = 0.1f;
        positionTimer.Autostart = false;
        positionTimer.OneShot = true;
        AddChild(positionTimer);
        positionTimer.Connect("timeout",this,"SendPosition");

        thrust = 0.0f;
        steer = 0.0f;

        // FIXME: Simple sprite management
        midground = GetNode<Node2D>("Midground");
        vessels = new Dictionary<int, Vessel>();

        vessel = GetNode<Vessel>("Foreground/Vessel");
        vessel.GetNode<CollisionShape2D>("Area/Hitbox").Disabled = true; // FIXME: Replace with different mask layer later!
        soundEmission = vessel.GetNode<SoundEmission>("SoundEmission");
        soundEmission.Connect("WaveReceivedBy",this,"SendSoundwaveCollision");

        sweep = GetNode<Sprite>("Foreground/Sweep");

        // AudioStreamGenerator testing...
        /*AudioStreamPlayer audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        AudioStreamGeneratorPlayback playback = audioStreamPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;

        _effect = (AudioEffectCapture)AudioServer.GetBusEffect(1, 1);
        _effect.BufferLength = 1.0f;

        for (int i = 0; i < playback.GetFramesAvailable(); i++)
        {
            //playback.PushFrame(Vector2.Zero);
        }

        audioStreamPlayer.Play();*/
    }

    /*private void ReceivedFrame(Vector2 init_frame)
    {
        GD.Print("Received frame!");//frames.Add(init_frame);
    }*/

    public override void _Process(float delta)
    {
        // FIXME: Deactivated to check other network changes in isolation

        // Take in player controls
        UpdatePosition(delta);

        // Move all objects on screen to h.c.state positions
        Render();

        // AudioStreamGenerator testing...
        /*AudioStreamPlayer audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        AudioStreamGeneratorPlayback playback = audioStreamPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;

        List<Vector2> sendFrames = new List<Vector2>(_effect.GetBuffer(_effect.GetFramesAvailable()));
        //_effect.ClearBuffer();

        for (int i = 0; i < sendFrames.Count; i++)
        {
            if (Input.IsActionPressed("ui_talk"))
            {
                h.c.Send4190(sendFrames[i].x,sendFrames[i].y);
            }
        }

        //var to_fill = playback.GetFramesAvailable();
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
        frames = new List<Vector2>();

        audioStreamPlayer.Play();*/
    }
    
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        int submarineID = handler.client.submarineID;
        Dictionary<int,Submarine> submarines = handler.client.state.GetSubmarines();

        //if (!submarines.ContainsKey(submarineID) || submarineID < 0)
        //    return;

        bool dot = Input.IsActionJustPressed("ui_dot");
        bool dash = Input.IsActionJustPressed("ui_dash");
        if (dot || dash)
            soundEmission.EmitSoundwave(dot);
    }

    public void UpdatePosition(float delta) // Interpolate using timestamp since last sighting?
    {
        Dictionary<int, Submarine> submarines = handler.client.state.GetSubmarines();

        if (!submarines.ContainsKey(handler.client.submarineID))
            return;

        Submarine submarine = submarines[handler.client.submarineID];
        float x = submarine.x[2];
        float y = submarine.y[2];
        float theta = submarine.theta[2];

        bool up = Input.IsActionPressed("ui_accelerate");
        bool down = Input.IsActionPressed("ui_decelerate");
        bool left = Input.IsActionPressed("ui_port");
        bool right = Input.IsActionPressed("ui_starboard");

        thrust = 0.0f;
        thrust += (up) ? 1.0f : 0.0f;
        thrust -= (down) ? 1.0f : 0.0f;

        steer = 0.0f;
        steer += (left) ? Mathf.Pi/16 : 0.0f;
        steer -= (right) ? Mathf.Pi/16 : 0.0f;

        submarine.DerivePosition(thrust,steer,delta);

        // FIXME: Even with this restriction, once the submarine starts it isn't likely to stop...
        if (positionTimer.IsStopped() && (x != submarine.x[2] || y != submarine.y[2] || theta != submarine.theta[2]))
            positionTimer.Start();
    }

    public void Render()
    {
        const float SWEEP_PERIOD = 6.0f;
        const float VISIBLE_PERIOD = 5.4f;

        int submarineID = handler.client.submarineID;
        Dictionary<int,Submarine> submarines = handler.client.state.GetSubmarines();

        if (!submarines.ContainsKey(submarineID) || submarineID < 0)
            return;

        float x = submarines[submarineID].x[2];
        float y = submarines[submarineID].y[2];
        float theta = submarines[submarineID].theta[2];

        long timestamp = DateTime.UtcNow.Ticks+handler.client.delay;
        long ftimestamp = (timestamp-handler.client.GetStarted())%(int)(SWEEP_PERIOD*Mathf.Pow(10,7));

        float sweepTheta = 2*Mathf.Pi*ftimestamp/(SWEEP_PERIOD*Mathf.Pow(10,7))-Mathf.Pi; 
        sweep.Rotation = sweepTheta+3*Mathf.Pi/2;
        
        foreach (int id in submarines.Keys)
        {
            if (id == submarineID)
                continue;

            (float x, float y, float theta) prediction = submarines[id].InterpolatePosition(timestamp);

            if (!vessels.ContainsKey(id))
            {
                vessels.Add(id,ResourceLoader.Load<PackedScene>("res://scenes/Vessel.tscn").Instance<Vessel>());
                midground.AddChild(vessels[id]);
                
                vessels[id].submarineID = id;
            }

            vessels[id].Position = new Vector2(prediction.x-x,prediction.y-y).Rotated(-theta)+vessel.Position;
            vessels[id].Rotation = prediction.theta-theta;

            // DEBUG: Prediction turned off!
            //vessels[id].Position = new Vector2(submarines[id].x[2]-x,submarines[id].y[2]-y).Rotated(-theta);
            //vessels[id].Rotation = submarines[id].theta[2]-theta;

            float ftheta = Mathf.Atan2(vessels[id].Position.y-vessel.Position.y,vessels[id].Position.x-vessel.Position.x);
            if (ftheta > sweepTheta)
                ftheta -= 2*Mathf.Pi;
            
            float t = 1.0f-Mathf.Pow((SWEEP_PERIOD/VISIBLE_PERIOD)*(sweepTheta-ftheta)/(2*Mathf.Pi),1.8f);

            vessels[id].Modulate = new Color(1.0f,1.0f,1.0f,t);
        }
    }

    // Sending values from our client state to our server state
    public void SendPosition()
    {
        if (!handler.client.state.GetSubmarines().ContainsKey(handler.client.submarineID))
            return;

        Submarine submarine = handler.client.state.GetSubmarines()[handler.client.submarineID];
        handler.client.Send4101(submarine.x[2],submarine.y[2],submarine.theta[2],DateTime.UtcNow.Ticks+handler.client.delay);
    }

    public void SendSoundwaveCollision(int submarineID, long intervalTicks)
    {
        // DEBUG:
        GD.Print(submarineID+" receives soundwave at "+intervalTicks+"...");

        if (!handler.client.state.GetSubmarines().ContainsKey(submarineID))
            return;
    }
}
