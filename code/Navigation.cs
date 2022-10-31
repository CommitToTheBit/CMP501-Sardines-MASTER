using Godot;
using System;
using System.Collections.Generic;

public class Navigation : Control
{
    private Handler h;
    private Timer positionTimer;

    // FIXME: Simple sprite set-up
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

        Submarine submarine = h.c.state.GetSubmarines()[h.c.GetClientID()];

        submarine.gas = (Input.IsActionPressed("ui_up")) ? 1 : 0;
        submarine.brakes = (Input.IsActionPressed("ui_down")) ? 1 : 0;
        submarine.steer += (Input.IsActionPressed("ui_left")) ? -1000*delta : 0;
        submarine.steer += (Input.IsActionPressed("ui_right")) ? 1000*delta : 0;
        submarine.steer = Mathf.Clamp(submarine.steer,-Mathf.Pi/2,Mathf.Pi/2);

        submarine.a = submarine.gas-submarine.brakes;
        submarine.u += 10.0f*delta*submarine.a; 

        Vector2 position = new Vector2(submarine.x,submarine.y);

        Vector2 prow = position+submarine.Structure*Vector2.Right.Rotated(Mathf.Pi*submarine.theta/180.0f);
        prow += delta*submarine.u*Vector2.Right.Rotated(submarine.theta+Mathf.Pi/2);
        
        Vector2 rudder = position-submarine.Structure*Vector2.Right.Rotated(Mathf.Pi*submarine.theta/180.0f);
        rudder += delta*submarine.u*Vector2.Right.Rotated(submarine.theta+submarine.steer+Mathf.Pi/2);

        submarine.x = 0.5f*(prow+rudder).x;
        submarine.y = 0.5f*(prow+rudder).y;
        submarine.theta = Mathf.Atan2(prow.y-rudder.y, prow.x-rudder.x);
    }

    public void Render()
    {
        int clientID = h.c.GetClientID();
        Dictionary<int,Submarine> submarines = h.c.state.GetSubmarines();

        if (!submarines.ContainsKey(clientID) || clientID < 0)
            return;

        Vector2 origin = Vector2.Zero;
        Vector2 clientPosition = new Vector2(submarines[clientID].x,submarines[clientID].y);
        float clientRotation = submarines[clientID].theta;
        
        foreach (int id in submarines.Keys)
        {
            if (id == clientID)
                continue;

            Vector2 position = new Vector2(submarines[id].x,submarines[id].y);
            float rotation = submarines[id].theta;

            if (!vessels.ContainsKey(id))
            {
                vessels.Add(id,ResourceLoader.Load<PackedScene>("res://scenes/Vessel.tscn").Instance<Vessel>());
                midground.AddChild(vessels[id]);
            }

            vessels[id].Position = (position-clientPosition).Rotated(-clientRotation)+origin;
            vessels[id].Rotation = rotation-clientRotation;
        }
    }

    // Sending values from our client state to our server state
    public void SendPosition()
    {
        GD.Print("go!");
        Submarine submarine = h.c.state.GetSubmarines()[h.c.GetClientID()];
        h.c.SendSubmarinePacket(h.c.GetClientID(),submarine.gas,submarine.brakes,submarine.steer,submarine.a,submarine.u,submarine.x,submarine.y,submarine.theta);
    }
}
