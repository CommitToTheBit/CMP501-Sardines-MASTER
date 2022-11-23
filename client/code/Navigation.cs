using Godot;
using System;
using System.Collections.Generic;

public class Navigation : Control
{
    private Handler h;
    private Timer positionTimer;

    // Submarine class control:
    float thrust;
    float steer;


    // FIXME: Simple sprite set-up
    Node2D foreground;
    Node2D midground;
    Dictionary<int,Vessel> vessels;

    Vessel vessel;

    public override void _Ready()
    {
        h = GetNode<Handler>("/root/Handler");

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
        midground = GetNode<Node2D>("Sonar/Midground");
        vessels = new Dictionary<int, Vessel>();

        vessel = GetNode<Vessel>("Sonar/Foreground/Vessel");

    }

    public override void _Process(float delta)
    {
        // Take in player controls
        UpdatePosition(delta);

        // Move all objects on screen to h.c.state positions
        Render();
    }

    public void UpdatePosition(float delta) // Interpolate using timestamp since last sighting?
    {
        Dictionary<int, Submarine> submarines = h.c.state.GetSubmarines();
        if (!submarines.ContainsKey(h.c.GetClientID()))
            return;

        Submarine submarine = submarines[h.c.GetClientID()];
        float x = submarine.x[2];
        float y = submarine.y[2];
        float theta = submarine.theta[2];

        bool up = Input.IsActionPressed("ui_up");
        bool down = Input.IsActionPressed("ui_down");
        bool left = Input.IsActionPressed("ui_left");
        bool right = Input.IsActionPressed("ui_right");

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
        const float VISIBLE_PERIOD = 12.0f;

        int clientID = h.c.GetClientID();
        Dictionary<int,Submarine> submarines = h.c.state.GetSubmarines();

        if (!submarines.ContainsKey(clientID) || clientID < 0)
            return;

        float x = submarines[clientID].x[2];
        float y = submarines[clientID].y[2];
        float theta = submarines[clientID].theta[2];

        long timestamp = DateTime.UtcNow.Ticks+h.c.delay;
        long ftimestamp = (timestamp-h.c.GetStarted())%(int)(SWEEP_PERIOD*Mathf.Pow(10,7));

        float sweep = 2*Mathf.Pi*ftimestamp/(SWEEP_PERIOD*Mathf.Pow(10,7))-Mathf.Pi; 
        
        foreach (int id in submarines.Keys)
        {
            if (id == clientID)
                continue;

            (float x, float y, float theta) prediction = submarines[id].QuadraticPredictPosition(timestamp);

            if (!vessels.ContainsKey(id))
            {
                vessels.Add(id,ResourceLoader.Load<PackedScene>("res://scenes/Vessel.tscn").Instance<Vessel>());
                midground.AddChild(vessels[id]);
            }

            vessels[id].Position = new Vector2(prediction.x-x,prediction.y-y).Rotated(-theta);
            vessels[id].Rotation = prediction.theta-theta;

            // DEBUG: Prediction turned off!
            //vessels[id].Position = new Vector2(submarines[id].x[2]-x,submarines[id].y[2]-y).Rotated(-theta);
            //vessels[id].Rotation = submarines[id].theta[2]-theta;

            float ftheta = Mathf.Atan2(vessels[id].Position.y,vessels[id].Position.x);
            if (ftheta > sweep)
                ftheta -= 2*Mathf.Pi;
            
            float t = 1.0f-(SWEEP_PERIOD/VISIBLE_PERIOD)*(sweep-ftheta)/(2*Mathf.Pi);

            vessels[id].Modulate = new Color(1.0f,1.0f,1.0f,t);
        }
    }

    // Sending values from our client state to our server state
    public void SendPosition()
    {
        Submarine submarine = h.c.state.GetSubmarines()[h.c.GetClientID()];
        h.c.SendPositionPacket(submarine.x[2],submarine.y[2],submarine.theta[2],DateTime.UtcNow.Ticks+h.c.delay);
    }
}
