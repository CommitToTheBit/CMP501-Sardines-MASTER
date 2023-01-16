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

    // Need to ensure all other submarines come to rest, on account of quadratic prediction...
    bool sending;
    float[] xSent, ySent;
    float[] thetaSent;

    // FIXME: Simple sprite set-up
    Node2D foreground;
    Node2D midground;
    Dictionary<int,Vessel> vessels;

    Vessel vessel;
    SoundEmission soundEmission;
    PackedScene soundwavePackedScene;

    Sprite sweep;

    // FIXME: AudioFrames handling
    private List<Vector2> frames;
    private AudioEffectCapture _effect;

    public override void _Ready()
    {
        handler = GetNode<Handler>("/root/Handler");
        handler.client.Connect("ReceivedSoundwaveCollision",this,"ReceiveSoundwaveCollision");
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

        sending = true;
        xSent = new float[3] { 0.0f, 0.0f, 0.0f };
        ySent = new float[3] { 0.0f, 0.0f, 0.0f };
        thetaSent = new float[3] { 0.0f, 0.0f, 0.0f };

        // FIXME: Simple sprite management
        midground = GetNode<Node2D>("Midground");
        vessels = new Dictionary<int, Vessel>();

        vessel = GetNode<Vessel>("Foreground/Vessel");
        vessel.GetNode<CollisionShape2D>("Area/Hitbox").Disabled = true; // FIXME: Replace with different mask layer later!
        soundEmission = vessel.GetNode<SoundEmission>("SoundEmission");
        soundEmission.Connect("WaveReceivedBy",this,"SendSoundwaveCollision");
        soundwavePackedScene = ResourceLoader.Load<PackedScene>("res://scenes/Soundwave.tscn");

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

        if (!submarines.ContainsKey(submarineID) || submarineID < 0)
            return;

        soundEmission.thetaRange += (Input.IsActionJustPressed("ui_widen")) ? 15.0f : 0.0f;
        soundEmission.thetaRange -= (Input.IsActionJustPressed("ui_shrink")) ? 15.0f : 0.0f;
        soundEmission.thetaRange = Mathf.Clamp(soundEmission.thetaRange,60.0f,360.0f);

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

        thrust = 0.0f; // NB: Change in second-order quantity
        thrust += (up) ? 1.0f : 0.0f;
        thrust -= (down) ? 1.0f : 0.0f;

        steer = 0.0f; // NB: Change in first-order quantity
        steer += (left) ? 1.0f : 0.0f;
        steer -= (right) ? 1.0f : 0.0f;

        submarine.DerivePosition(thrust,steer,delta);

        // FIXME: Even with this restriction, once the submarine starts it isn't likely to stop...
        if (positionTimer.IsStopped() && ((x != submarine.x[2] || y != submarine.y[2] || theta != submarine.theta[2]) || !sending)) // sending is used to re-establish rest, preventing 'long-term' prediction! 
            positionTimer.Start();

        //GD.Print(stopped);
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

        long timestamp = submarines[submarineID].timestamp[2];//-handler.client.delay; // Convert external players back to 'local' client time
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
        handler.client.Send4101(submarine.x[2],submarine.y[2],submarine.theta[2],submarine.timestamp[2]);//+handler.client.delay);

        xSent[0] = xSent[1];
        xSent[1] = xSent[2];
        xSent[2] = submarine.x[2];

        ySent[0] = ySent[1];
        ySent[1] = ySent[2];
        ySent[2] = submarine.y[2];

        thetaSent[0] = thetaSent[1];
        thetaSent[1] = thetaSent[2];
        thetaSent[2] = submarine.theta[2];

        sending = xSent[0] == xSent[1] && xSent[1] == xSent[2] && ySent[0] == ySent[1] && ySent[1] == ySent[2] && thetaSent[0] == thetaSent[1] && thetaSent[1] == thetaSent[2];
    }

    public void SendSoundwaveCollision(int receiverID, bool collisionDot, float collisionRange, float collisionAngle, long collisionInterval)
    {
        // Filters out any 'null' submarines
        if (handler.client.state.GetSubmarines()[receiverID].captain.clientID < 0)
            return;

        handler.client.Send4102(handler.client.submarineID,receiverID,collisionDot,collisionRange,collisionAngle,collisionInterval);
    }

    public void ReceiveSoundwaveCollision(int senderID, bool collisionDot, float collisionRange, float collisionAngle, long collisionInterval)
    {
        // DEBUG:
        GD.Print(senderID+" sent a "+((collisionDot) ? "dot" : "dash")+" at angle "+collisionAngle+" after "+collisionInterval+" ticks...");

        // Filters out any 'null' submarines
        if (handler.client.state.GetSubmarines()[senderID].captain.clientID < 0)
            return;

        Submarine sender = handler.client.state.GetSubmarines()[senderID];
        Submarine receiver = handler.client.state.GetSubmarines()[handler.client.submarineID];

        long timestamp = receiver.timestamp[2];//DateTime.UtcNow.Ticks;//+handler.client.delay;
        (float x, float y, float theta) sent = sender.InterpolatePosition(timestamp-collisionInterval);

        // Spawn soundwave
        Soundwave soundwave = ResourceLoader.Load<PackedScene>("res://scenes/Soundwave.tscn").Instance<Soundwave>();
        AddChild(soundwave);

        soundwave.Position = new Vector2(sent.x-receiver.x[2],sent.y-receiver.y[2]).Rotated(-receiver.theta[2])+vessel.Position;
        soundwave.Rotation = Mathf.Atan2((vessel.Position.x-soundwave.Position.x),-(vessel.Position.y-soundwave.Position.y))-collisionAngle;

        float r_range = (soundwave.Position-vessel.Position).Length();
        r_range += (collisionDot) ? 0.5f*Soundwave.DOT_WIDTH : 0.5f*Soundwave.DASH_WIDTH;
        soundwave.PropagateWave(r_range,r_range,collisionDot,collisionRange,0.42f,false);
    }
}
