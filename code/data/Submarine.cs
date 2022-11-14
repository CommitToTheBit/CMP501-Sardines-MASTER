using Godot;
using System;

public class Submarine
{
    // Constants
    private const float structure = 20.0f;
    public float Structure
    {
        get { return structure; }
    }

    // Public variables: sent to/from server in submarine packet
    public float[] x, y;
    public float[] theta;
    public long[] timestamp; // Measured in ticks; 1 second == 10,000,000 ticks

    // Private physics variables: used to handle the physics of a player-controlled submarine
    private float a;
    private float u;

    // Private prediction variables: parameters for our quadratic models
    private float[] X, Y;
    private float[] THETA;
    private long TIMESTAMP_0; 

    // Constructor
    public Submarine(float init_x, float init_y, float init_theta, long init_timestamp)
    {
        // Initialise position of submarine
        x = new float[3] {init_x, init_x, init_x};
        y = new float[3] {init_y, init_y, init_y};
        theta = new float[3] {init_theta, init_theta, init_theta};
        timestamp = new long[3] {init_timestamp, init_timestamp-1, init_timestamp-2}; // 'Cheat' timestamps to avoid division by zero

        // Initialise submarine to start at rest (only affects player-controlled submarine)
        a = 0.0f;
        u = 0.0f;

        // Initialise prediction variables via UpdateQuadraticModel
        UpdateQuadraticModel();
    }

    // Destructor
    ~Submarine()
    {

    }

    // 'Logging' updates to position
    public void UpdatePosition(float init_x, float init_y, float init_theta, long init_timestamp)
    {
        // Disregard any position updates sent out of order (makes no sense to factor something the player hasn't seen into any model!)
        if (init_timestamp <= timestamp[0])
            return;

        x = new float[3] {init_x, x[0], x[1]};
        y = new float[3] {init_y, y[0], y[1]};
        theta = new float[3] {init_theta, theta[0], theta[1]};
        timestamp = new long[3] {init_timestamp, timestamp[0], timestamp[1]};        
    }

    // Physics
    public void DerivePosition(float thrust, float steer, float delta)
    {
        const float conversion = 10.0f;

        a += conversion*delta*thrust;
        u += delta*a;
    
        // FIXME: Use of timestamp[0]+delta here could be shaky if sending/receiving own position?

 
    }

    // Prediction
    public void UpdateQuadraticModel() // No inputs, as updating from the submarine's logged positions
    {

    }
}