using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class Screen : Control
{
    private Handler h;
    private Timer positionTimer;

    // FIXME: Simple sprite set-up
    Sprite sprite;
    Dictionary<int,Sprite> sprites;

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
        sprite = GetNode<Sprite>("Sprite");
        sprites = new Dictionary<int, Sprite>();

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

        Vector2 origin = 0.5f*GetViewport().Size;
        Vector2 clientPosition = new Vector2(submarines[clientID].GetX(),submarines[clientID].GetY());
        float clientRotation = submarines[clientID].GetDirection();
        
        foreach (int id in submarines.Keys)
        {
            Vector2 position = new Vector2(submarines[id].GetX(),submarines[id].GetY());
            float rotation = submarines[id].GetDirection();

            if (!sprites.ContainsKey(id))
            {
                sprites.Add(id,(Sprite)sprite.Duplicate());
                AddChild(sprites[id]);
            }

            sprites[id].Position = (position-clientPosition).Rotated(-clientRotation)+origin;
            sprites[id].Rotation = rotation-clientRotation;
        }
    }

    // Sending values from our client state to our server state
    public void SendPosition()
    {
        Submarine submarine = h.c.state.GetSubmarines()[h.c.GetClientID()];
        h.c.SendSubmarinePacket(submarine.GetX(),submarine.GetY(),submarine.GetDirection(),submarine.GetSteer());
    }
}
