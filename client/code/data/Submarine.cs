using Godot;
using System;

public class Submarine
{
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
    private long TIMESTAMP; 

    // Constructor
    public Submarine(float init_x, float init_y, float init_theta, long init_timestamp)
    {
        // Initialise position of submarine
        x = new float[3] {init_x, init_x, init_x};
        y = new float[3] {init_y, init_y, init_y};
        theta = new float[3] {init_theta, init_theta, init_theta};
        timestamp = new long[3] {init_timestamp-2, init_timestamp-1, init_timestamp}; // 'Cheat' timestamps to avoid division by zero

        // Initialise submarine to start at rest (only affects player-controlled submarine)
        a = 0.0f;
        u = 0.0f;

        // Initialise prediction variables via UpdateQuadraticModel
        UpdatePredictionModel();
    }

    // Copy Constructor
    public Submarine(Submarine init_submarine)
    {
        x = init_submarine.x;
        y = init_submarine.y;
        theta = init_submarine.theta;
        timestamp = init_submarine.timestamp;

        a = init_submarine.a;
        u = init_submarine.u;

        X = init_submarine.X;
        Y = init_submarine.Y;
        THETA = init_submarine.THETA;
        TIMESTAMP = init_submarine.TIMESTAMP;
    }

    // Destructor
    ~Submarine()
    {

    }

    // 'Logging' updates to position
    public bool UpdatePosition(float init_x, float init_y, float init_theta, long init_timestamp)
    {
        // Disregard any position updates sent out of order (makes no sense to factor something the player hasn't seen into any model!)
        if (init_timestamp <= timestamp[2])
            return false;

        x = new float[3] {x[1], x[2], init_x};
        y = new float[3] {y[1], y[2], init_y};
        theta = new float[3] {theta[1], theta[2], init_theta};
        timestamp = new long[3] {timestamp[1], timestamp[2], init_timestamp}; 

        return true;      
    }

    // Physics
    public void DerivePosition(float thrust, float steer, float delta)
    {
        const float conversion = 10.0f;
        const float length = 40.0f;

        //UpdatePosition(x[2]-50.0f*delta*steer,y[2]-50.0f*delta*thrust,0.0f,timestamp[2]+(long)(Mathf.Pow(10,7)*delta));
        //return;

        // FIXME: Calculate x/y accelaterations, velocities *analytically*
        //a += conversion*delta*thrust;
        a = thrust-Mathf.Pow(u/40.0f,2);
        u += 10.0f*delta*a;
        u = Mathf.Clamp(u,0,40.0f);
        //u = 20.0f;

        float xFront = x[2]+0.5f*length*Mathf.Sin(theta[2]); // x-coordinate of front of submarine
        xFront += delta*u*Mathf.Sin(theta[2]); // Horizontal movement

        float yFront = y[2]+0.5f*(-length*Mathf.Cos(theta[2])); // y-coordinate of front of submarine
        yFront += delta*u*(-Mathf.Cos(theta[2])); // Vertical movement

        float xBack = x[2]-0.5f*length*Mathf.Sin(theta[2]); // x-coordinate of back of submarine
        xBack += delta*u*Mathf.Sin(theta[2]+steer); // Horizontal movement

        float yBack = y[2]-0.5f*(-length*Mathf.Cos(theta[2])); // y-coordinate of back of submarine
        yBack += delta*u*(-Mathf.Cos(theta[2]+steer)); // Vertical movement

        //GD.Print("y comparison: "+0.5f*(yFront+yBack)+" vs. previous "+y[2]);

        // Set this as the player's new position (this derivation will always be true in resolving disputes)
        // FIXME: Use of timestamp[0]+delta here could be shaky if sending/receiving own position?
        UpdatePosition(0.5f*(xFront+xBack),0.5f*(yFront+yBack),Mathf.Atan2(xFront-xBack,-yFront+yBack),timestamp[2]+(long)(Mathf.Pow(10,7)*delta));

        // No need to update quadratic model, as we will never have to predict our own position!
    }

    // Prediction
    public void UpdatePredictionModel() // No inputs, as updating from the submarine's logged positions
    {
        // Timestamps converted to seconds
        float[] deltas = new float[2] { Mathf.Pow(10,-7)*(timestamp[1]-timestamp[0]), Mathf.Pow(10,-7)*(timestamp[2]-timestamp[1]) };

        // Averaging velocities and accelerations
        float[] ux = new float[2] { (x[1]-x[0])/deltas[0], (x[2]-x[1])/deltas[1] };
        float[] ax = new float[1] { (ux[1]-ux[0])/deltas[0] };

        float[] uy = new float[2] { (y[1]-y[0])/deltas[0], (y[2]-y[1])/deltas[1] };
        float[] ay = new float[1] { (uy[1]-uy[0])/deltas[0] };

        float[] utheta = new float[2] { (theta[1]-theta[0])/deltas[0], (theta[2]-theta[1])/deltas[1] };
        float[] atheta = new float[1] { (utheta[1]-utheta[0])/deltas[0] };

        // Update parameters of quadratic model
        X = new float[3] { x[2], ux[1], ax[0] };
        Y = new float[3] { y[2], uy[1], ay[0] };
        THETA = new float[3] { theta[2], utheta[1], atheta[0] };
        TIMESTAMP = timestamp[2];
    }

    public (float xPrediction, float yPrediction, float thetaPrediction) QuadraticPredictPosition(long timestampPrediction)
    {
        // Derive delta from timestamps
        float t = Mathf.Pow(10,-7)*(timestampPrediction-TIMESTAMP);

        // Quadratically predict positions at time t
        float xPrediction = X[0]+X[1]*t+0.5f*X[2]*t*t;
        float yPrediction = Y[0]+Y[1]*t+0.5f*Y[2]*t*t;
        float thetaPrediction = THETA[0]+THETA[1]*t+0.5f*THETA[2]*t*t; 

        return (xPrediction: xPrediction, yPrediction: yPrediction, thetaPrediction: thetaPrediction);
    }
}