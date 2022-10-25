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
    private Timer connectionTimer;
    private Timer positionTimer;
    private bool dead;

    private static int HEADERSIZE = Marshal.SizeOf(new HeaderPacket());

    Label messenger;

    int counter = 0;

    Sprite sprite;

    public override void _Ready()
    {
        tcpConnection = new TCPConnection();
        dead = true;

        messenger = GetNode<Label>("Messenger");

        connectionTimer = new Timer();
        connectionTimer.WaitTime = 0.1f;
        connectionTimer.Autostart = false;
        connectionTimer.OneShot = false;
        AddChild(connectionTimer);
        connectionTimer.Connect("timeout",this,"CheckConnections");
        connectionTimer.Start();

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
        PositionPacket position = new PositionPacket(0,sprite.Position.x,sprite.Position.y);
        SendablePacket packet = new SendablePacket(new HeaderPacket(1),Packet.Serialise<PositionPacket>(position));
        tcpConnection.SendPacket(packet);
    }

    public void CheckConnections()
    {
        if (dead)
        {
            dead = tcpConnection.Connect();
            return;
        }

        // The structure that describes the set of sockets we're interested in reading from.
        List<Socket> readable = new List<Socket>();

        // The structure that describes the set of sockets we're interested in writing to.
        List<Socket> writeable = new List<Socket>();

        if (tcpConnection.IsRead())
        {
            readable.Add(tcpConnection.GetSocket());
        }
        if (tcpConnection.IsWrite())
        {
            writeable.Add(tcpConnection.GetSocket());
        }

        Socket.Select(readable, writeable, null, 0);
        //Console.WriteLine("1 connection, " + (readable.Count + writeable.Count) + " are ready.");

        if (readable.Contains(tcpConnection.GetSocket()))
        {
            //GD.Print("Trying to read...");
            dead |= tcpConnection.Read();
            if (tcpConnection.isRecvPacket())
            {
                SendablePacket packet = tcpConnection.RecvPacket();
                GD.Print("Received a packet with bodyID "+packet.header.bodyID.ToString());
                if (packet.header.bodyID == 1)
                {
                    PositionPacket position = Packet.Deserialise<PositionPacket>(packet.serialisedBody);
                    Console.WriteLine("This is a PositionPacket saying object "+position.objectID.ToString()+" has coordinates ("+position.x.ToString()+", "+position.y.ToString()+")");
                    //sprite.Position = new Vector2(position.x,position.y);
                }
            }
        }

        if (writeable.Contains(tcpConnection.GetSocket()))
        {
            //GD.Print("Trying to write...");
            dead |= tcpConnection.Write();
        }
    }
}
