using Godot;
using System;

public class Vessel : AnimatedSprite
{
    public int submarineID;

    public override void _Ready()
    {
        submarineID = -1;
    }
}
