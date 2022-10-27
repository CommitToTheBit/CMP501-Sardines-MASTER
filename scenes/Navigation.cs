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
        if (Input.IsActionPressed("ui_up"))
        {
            Submarine submarine = h.c.state.GetSubmarines()[h.c.GetClientID()];
            h.c.state.MoveSubmarine(h.c.GetClientID(),300*delta*Mathf.Sin(submarine.GetDirection()),-300*delta*Mathf.Cos(submarine.GetDirection()),0.0f,0.0f);
            if (positionTimer.IsStopped())
                positionTimer.Start();
        }
        if (Input.IsActionPressed("ui_down"))
        {
            Submarine submarine = h.c.state.GetSubmarines()[h.c.GetClientID()];
            h.c.state.MoveSubmarine(h.c.GetClientID(),-300*delta*Mathf.Sin(submarine.GetDirection()),300*delta*Mathf.Cos(submarine.GetDirection()),0.0f,0.0f);
            if (positionTimer.IsStopped())
                positionTimer.Start();
        }
        if (Input.IsActionPressed("ui_left"))
        {
            h.c.state.MoveSubmarine(h.c.GetClientID(),0.0f,0.0f,-delta,0.0f);
            if (positionTimer.IsStopped())
                positionTimer.Start();
        }
        if (Input.IsActionPressed("ui_right"))
        {
            h.c.state.MoveSubmarine(h.c.GetClientID(),0.0f,0.0f,delta,0.0f);
            if (positionTimer.IsStopped())
                positionTimer.Start();
        }
    }

    public void Render()
    {
        int clientID = h.c.GetClientID();
        Dictionary<int,Submarine> submarines = h.c.state.GetSubmarines();

        if (!submarines.ContainsKey(clientID) || clientID < 0)
            return;

        Vector2 origin = Vector2.Zero;
        Vector2 clientPosition = new Vector2(submarines[clientID].GetX(),submarines[clientID].GetY());
        float clientRotation = submarines[clientID].GetDirection();
        
        foreach (int id in submarines.Keys)
        {
            if (id == clientID)
                continue;

            Vector2 position = new Vector2(submarines[id].GetX(),submarines[id].GetY());
            float rotation = submarines[id].GetDirection();

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
        Submarine submarine = h.c.state.GetSubmarines()[h.c.GetClientID()];
        h.c.SendSubmarinePacket(submarine.GetX(),submarine.GetY(),submarine.GetDirection(),submarine.GetSteer());
    }
}
