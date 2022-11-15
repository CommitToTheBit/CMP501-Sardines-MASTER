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

        Submarine submarine = submarines[h.c.GetClientID()];

        submarine.gas = (Input.IsActionPressed("ui_up")) ? 1 : 0;
        submarine.brakes = (Input.IsActionPressed("ui_down")) ? 1 : 0;
        submarine.steer = (Input.IsActionPressed("ui_left")) ? 10.0f/*1.0f*delta*/ : 0;
        submarine.steer += (Input.IsActionPressed("ui_right")) ? -10.0f/*-1.0f*delta*/ : 0;
        submarine.steer = Mathf.Clamp(submarine.steer,-Mathf.Pi/16,Mathf.Pi/16);

        submarine.a += delta*(submarine.gas-submarine.brakes);
        submarine.u += 10.0f*delta*submarine.a; 
        //submarine.u = 50.0f;

        Vector2 position = new Vector2(submarine.x,submarine.y);

        Vector2 prow = position+submarine.Structure*Vector2.Up.Rotated(submarine.theta);
        prow += delta*submarine.u*Vector2.Up.Rotated(submarine.theta);
        
        Vector2 rudder = position-submarine.Structure*Vector2.Up.Rotated(submarine.theta);
        rudder += delta*submarine.u*Vector2.Up.Rotated(submarine.theta+submarine.steer);

        //GD.Print(180.0f*(submarine.theta+submarine.steer)/Mathf.Pi);

        submarine.x = 0.5f*(prow+rudder).x;
        submarine.y = 0.5f*(prow+rudder).y;
        submarine.theta = Mathf.Atan2(prow.x-rudder.x,-prow.y+rudder.y);//-Mathf.Pi;
        //GD.Print((prow.y-rudder.y)*(prow.x-rudder.x));

        submarine.t0 = DateTime.Now.Ticks;

        if (positionTimer.IsStopped())
            positionTimer.Start();
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

            //Vector2 position = new Vector2(submarine[id].x,submarine.y);
            long t = DateTime.Now.Ticks;

            Vector2 prow = position+submarines[id].Structure*Vector2.Up.Rotated(submarines[id].theta);
            prow += 0.0000001f*(t-submarines[id].t0)*submarines[id].u*Vector2.Up.Rotated(submarines[id].theta);
            
            Vector2 rudder = position-submarines[id].Structure*Vector2.Up.Rotated(submarines[id].theta);
            rudder += 0.0000001f*(t-submarines[id].t0)*submarines[id].u*Vector2.Up.Rotated(submarines[id].theta+submarines[id].steer);   

            //GD.Print(180.0f*(submarine.theta+submarine.steer)/Mathf.Pi);

            position = 0.5f*(prow+rudder);
            rotation = Mathf.Atan2(prow.x-rudder.x,-prow.y+rudder.y);

            //submarines[id].x = 0.5f*(prow+rudder).x;
            //submarine.y = 0.5f*(prow+rudder).y;
            //submarine.theta = Mathf.Atan2(prow.x-rudder.x,-prow.y+rudder.y);

            submarines[id].x = 0.5f*(prow+rudder).x;
            submarines[id].y = 0.5f*(prow+rudder).y;
            submarines[id].theta = Mathf.Atan2(prow.x-rudder.x,-prow.y+rudder.y);//-Mathf.Pi;
            //GD.Print((prow.y-rudder.y)*(prow.x-rudder.x));


            //GD.Print(0.0000001f*(t-submarines[id].t0));

            submarines[id].t0 = DateTime.Now.Ticks;

            vessels[id].Position = (position-clientPosition).Rotated(-clientRotation)+origin;
            vessels[id].Rotation = rotation-clientRotation;
        }
    }

    // Sending values from our client state to our server state
    public void SendPosition()
    {
        Submarine submarine = h.c.state.GetSubmarines()[h.c.GetClientID()];
        h.c.SendSubmarinePacket(h.c.GetClientID(),submarine.x,submarine.y,submarine.theta,DateTime.UtcNow.Ticks);
    }
}
