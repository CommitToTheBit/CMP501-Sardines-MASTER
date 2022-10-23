using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

struct PositionPacket
{
    public int objectID;
    public float x, y;
}