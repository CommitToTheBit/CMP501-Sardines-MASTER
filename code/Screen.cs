using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class Screen : Control
{
    private TCPConnection tcpConnection;
    private Timer positionTimer;
    private bool connected;

    private static int HEADERSIZE = Marshal.SizeOf(new HeaderPacket());


    Label messenger;

    int counter = 0;

    Sprite sprite;

    public override void _Ready()
    {
        tcpConnection = new TCPConnection();
        connected = false;

        messenger = GetNode<Label>("Messenger");

        positionTimer = new Timer();
        positionTimer.WaitTime = 1.0f;
        positionTimer.Autostart = false;
        positionTimer.OneShot = false;
        AddChild(positionTimer);
        positionTimer.Connect("timeout",this,"SendPositionPacket");

        sprite = GetNode<Sprite>("Sprite");

    }

    public override void _Process(float delta)
    {
        if (connected)
        {

        }
        else
        {
            connected = tcpConnection.Connect();
            positionTimer.Start();
        }

        if (Input.IsActionPressed("ui_up"))
        {
            sprite.Position += new Vector2(0.0f,-150*delta);
        }
        if (Input.IsActionPressed("ui_down"))
        {
            sprite.Position += new Vector2(0.0f,150*delta);
        }
        if (Input.IsActionPressed("ui_left"))
        {
            sprite.Position += new Vector2(-150*delta,0.0f);
        }
        if (Input.IsActionPressed("ui_right"))
        {
            sprite.Position += new Vector2(150*delta,0.0f);
        }
    }

    /*public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("ui_accept"))
        {
            tcpConnection.SetBuffer(messenger.Text.PadRight(MESSAGESIZE,'#'));

            tcpConnection.WriteFromBuffer();
            tcpConnection.ReadToBuffer();

            messenger.Text = Encoding.UTF8.GetString(tcpConnection.GetBuffer());
        }
    }*/

    public void SendPositionPacket()
    {
        GD.Print("timer running!");

        PositionPacket position = new PositionPacket(0,sprite.Position.x,sprite.Position.y);
        tcpConnection.Send(new SendablePacket(new HeaderPacket(1),Packet.Serialise<PositionPacket>(position)));
    }
}
