using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

struct PositionPacket
{
    int objectID;
    float x, y;
}