using Godot;
using System;

public class Submarine
{
    float x, y;

    public Submarine(float init_x, float init_y)
    {
        x = init_x;
        y = init_y;
    }

    ~Submarine()
    {

    }

    public float GetX()
    {
        return x;
    }

    public float GetY()
    {
        return y;
    }

    public void Update(float new_x, float new_y)
    {
        x = new_x;
        y = new_y;
    }
}
