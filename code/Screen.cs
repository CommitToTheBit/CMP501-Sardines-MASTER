using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class Screen : Control
{
    private Client client;
    private Timer clientTimer;
    private Timer positionTimer;
    private bool dead;

    private static int HEADERSIZE = Marshal.SizeOf(new HeaderPacket());

    Label messenger;

    int counter = 0;

    Sprite sprite;
    Dictionary<int,Sprite> sprites;

    public override void _Ready()
    {
        client = new Client();

        messenger = GetNode<Label>("Messenger");

        clientTimer = new Timer();
        clientTimer.WaitTime = 0.01f;
        clientTimer.Autostart = false;
        clientTimer.OneShot = false;
        AddChild(clientTimer);
        clientTimer.Connect("timeout",this,"ClientTick");
        clientTimer.Start();

        positionTimer = new Timer();
        positionTimer.WaitTime = 0.02f;
        positionTimer.Autostart = false;
        positionTimer.OneShot = true;
        AddChild(positionTimer);
        positionTimer.Connect("timeout",this,"SendPositionPacket");

        sprite = GetNode<Sprite>("Sprite");
        sprites = new Dictionary<int, Sprite>();

    }

    public override void _Process(float delta)
    {
        if (Input.IsActionPressed("ui_up"))
        {
            client.state.MoveSubmarine(client.GetClientID(),0.0f,-300*delta);
            if (positionTimer.IsStopped())
                positionTimer.Start();
        }
        if (Input.IsActionPressed("ui_down"))
        {
            client.state.MoveSubmarine(client.GetClientID(),0.0f,300*delta);
            if (positionTimer.IsStopped())
                positionTimer.Start();
        }
        if (Input.IsActionPressed("ui_left"))
        {
            client.state.MoveSubmarine(client.GetClientID(),-300*delta,0.0f);
            if (positionTimer.IsStopped())
                positionTimer.Start();
        }
        if (Input.IsActionPressed("ui_right"))
        {
            client.state.MoveSubmarine(client.GetClientID(),300*delta,0.0f);
            if (positionTimer.IsStopped())
                positionTimer.Start();
        }

        Render();
    }

    public void Render()
    {
        Dictionary<int,Submarine> submarines = client.state.GetSubmarines();

        foreach (int id in submarines.Keys)
        {
            if (!sprites.ContainsKey(id))
            {
                sprites.Add(id,(Sprite)sprite.Duplicate());
                AddChild(sprites[id]);
            }
            sprites[id].Position = new Vector2(submarines[id].GetX(),submarines[id].GetY());
        }
    }

    public void SendPositionPacket()
    {
        Submarine submarine = client.state.GetSubmarines()[client.GetClientID()];
        client.SendSubmarinePacket(submarine.GetX(),submarine.GetY());
    }

    public void ClientTick()
    {
        if (client.IsConnected())
        {
            client.Write();
            client.Read();
            client.Update();
        }
        else
        {
            client.Connect();
        }
    }
}
