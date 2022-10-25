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

    public override void _Ready()
    {
        client = new Client();

        messenger = GetNode<Label>("Messenger");

        clientTimer = new Timer();
        clientTimer.WaitTime = 0.1f;
        clientTimer.Autostart = false;
        clientTimer.OneShot = false;
        AddChild(clientTimer);
        clientTimer.Connect("timeout",this,"ClientTick");
        clientTimer.Start();

        positionTimer = new Timer();
        positionTimer.WaitTime = 1.0f;
        positionTimer.Autostart = false;
        positionTimer.OneShot = true;
        AddChild(positionTimer);
        positionTimer.Connect("timeout",this,"SendPositionPacket");

        sprite = GetNode<Sprite>("Sprite");

    }

    public override void _Process(float delta)
    {
        if (Input.IsActionPressed("ui_up"))
        {
            sprite.Position += new Vector2(0.0f,-300*delta);
            if (positionTimer.IsStopped())
                positionTimer.Start();
        }
        if (Input.IsActionPressed("ui_down"))
        {
            sprite.Position += new Vector2(0.0f,300*delta);
            if (positionTimer.IsStopped())
                positionTimer.Start();
        }
        if (Input.IsActionPressed("ui_left"))
        {
            sprite.Position += new Vector2(-300*delta,0.0f);
            if (positionTimer.IsStopped())
                positionTimer.Start();
        }
        if (Input.IsActionPressed("ui_right"))
        {
            sprite.Position += new Vector2(300*delta,0.0f);
            if (positionTimer.IsStopped())
                positionTimer.Start();
        }
    }

    public void SendPositionPacket()
    {
        PositionPacket position = new PositionPacket(0,sprite.Position.x,sprite.Position.y);
        SendablePacket packet = new SendablePacket(new HeaderPacket(1),Packet.Serialise<PositionPacket>(position));
        client.SendPacket(packet);
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
