using Godot;
using System;

public class Submarine
{
    float x, y;

    // CAR PHYSICS
    public float direction;
    public float steer;
    public float speed;
    public float structure; // What would this be?

    public Submarine(float init_x, float init_y, float init_direction, float init_steer)
    {
        x = init_x;
        y = init_y;

        direction = init_direction;
        steer = init_steer;
        speed = 300.0f;
        structure = 30.0f;
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

    public float GetDirection()
    {
        return direction;
    }

    public float GetSteer()
    {
        return steer;
    }

    public void Update(float new_x, float new_y, float new_direction, float new_steer)
    {
        x = new_x;
        y = new_y;
        direction = new_direction;
        steer = new_steer;
    }
}
