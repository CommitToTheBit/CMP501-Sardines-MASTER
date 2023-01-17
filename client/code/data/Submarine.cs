using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Submarine
{
    const float T_INTERPOLATION = 0.1f; // Interpolation period

    // Public status variables:
    public Crew captain;
    public Dictionary<int, Crew> crew;

    public bool nuclearCapability;
    public bool contactCapability;

    // Private status variables
    private bool positionInitialised; // FIXME: Make a private version workable!

    // Public variables: sent to/from server in submarine packet
    public float[] x, y;
    public float[] theta;
    public long[] timestamp; // Measured in ticks; 1 second == 10,000,000 ticks

    // Private physics variables: used to handle the physics of a player-controlled submarine
    private float a;
    private float u;
    private float rudder;

    // Private prediction variables: parameters for our quadratic models
    private float[][] X, Y;
    private float[][] THETA;
    private long[] TIMESTAMP;
    private long INTERPOLATION_TIMESTAMP;

    private float stationaryX, stationaryY;
    private float stationaryTheta;

    // Constructor
    public Submarine(int init_clientID, string init_clientIP, bool init_nuclearCapability)
    {
        captain = new Crew(init_clientID, init_clientIP);
        Dictionary<int, Crew> crew = new Dictionary<int, Crew>(); // FIXME: No crew mechanics implemented yet...

        nuclearCapability = init_nuclearCapability;
        contactCapability = true; // FIXME: Ignoring this mechanic - for now

        // Assign dummy entries 
        positionInitialised = false;

        x = new float[3] { 0.0f, 0.0f, 0.0f };
        y = new float[3] { 0.0f, 0.0f, 0.0f };
        theta = new float[3] { 0.0f, 0.0f, 0.0f };
        timestamp = new long[3] { -2, -1, 0 }; // 'Cheat' timestamps to avoid division by zero

        a = 0.0f;
        u = 0.0f;
        rudder = 0.0f;

        INTERPOLATION_TIMESTAMP = 0;

        UpdatePredictionModel(0);
    }

    // Copy Constructor
    public Submarine(Submarine init_submarine)
    {
        nuclearCapability = init_submarine.nuclearCapability;
        contactCapability = init_submarine.contactCapability;

        positionInitialised = init_submarine.positionInitialised;

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

    // Initialising position
    public bool InitialisePosition(float init_x, float init_y, float init_theta, long init_timestamp)
    {
        // Has position already been initialised? 
        if (positionInitialised)
            return UpdatePosition(init_x, init_y, init_theta, init_timestamp);

        // Initialise position of submarine
        stationaryX = init_x;
        stationaryY = init_y;
        stationaryTheta = init_theta;

        x = new float[3] { init_x, init_x, init_x };
        y = new float[3] { init_y, init_y, init_y };
        theta = new float[3] { init_theta, init_theta, init_theta };
        timestamp = new long[3] { init_timestamp - 2, init_timestamp - 1, init_timestamp }; // 'Cheat' timestamps to avoid division by zero

        // Initialise submarine to start at rest (only affects player-controlled submarine)
        a = 0.0f;
        u = 0.0f;
        rudder = 0.0f;

        // Initialise prediction variables via UpdateQuadraticModel
        UpdatePredictionModel(init_timestamp);

        positionInitialised = true;
        return true;
    }

    // 'Logging' updates to position
    public bool UpdatePosition(float init_x, float init_y, float init_theta, long init_timestamp, long init_delay = 0)
    {
        // If position is uninitialised, do that!
        if (!positionInitialised)
            return InitialisePosition(init_x, init_y, init_theta, init_timestamp-init_delay);

        // Disregard any position updates sent out of order (makes no sense to factor something the player hasn't seen into any model!)
        if (init_timestamp <= timestamp[2])
            return false;

        stationaryX = x[0];
        stationaryY = y[0];
        stationaryTheta = theta[0];

        x = new float[3] { x[1], x[2], init_x };
        y = new float[3] { y[1], y[2], init_y };
        theta = new float[3] { theta[1], theta[2], init_theta };
        timestamp = new long[3] { timestamp[1], timestamp[2], init_timestamp };

        return true;
    }

    // Physics
    public void DerivePosition(float thrust, float steer, float delta)
    {
        //const float conversion = 10.0f;
        const float length = 40.0f;

        //UpdatePosition(x[2]-50.0f*delta*steer,y[2]-50.0f*delta*thrust,0.0f,timestamp[2]+(long)(Mathf.Pow(10,7)*delta));
        //return;

        // FIXME: Calculate x/y accelaterations, velocities *analytically*
        //a += conversion*delta*thrust;
        a = thrust-Mathf.Pow(u/40.0f,2);
        u += 10.0f*delta*a;
        u = Mathf.Clamp(u,0.0f,40.0f);
        if (thrust <= 0.0f && u < 6.0f) // 'STOPPING THRESHOLD'
            u = 0.0f;

        rudder += (Mathf.Pi/16.0f)*delta*steer;
        rudder = Mathf.Clamp(rudder,-Mathf.Pi/8.0f,Mathf.Pi/8.0f);

        float xFront = x[2]+0.5f*length*Mathf.Sin(theta[2]); // x-coordinate of front of submarine
        xFront += delta*u*Mathf.Sin(theta[2]); // Horizontal movement

        float yFront = y[2]+0.5f*(-length*Mathf.Cos(theta[2])); // y-coordinate of front of submarine
        yFront += delta*u*(-Mathf.Cos(theta[2])); // Vertical movement

        float xBack = x[2]-0.5f*length*Mathf.Sin(theta[2]); // x-coordinate of back of submarine
        xBack += delta*u*Mathf.Sin(theta[2]+rudder); // Horizontal movement

        float yBack = y[2]-0.5f*(-length*Mathf.Cos(theta[2])); // y-coordinate of back of submarine
        yBack += delta*u*(-Mathf.Cos(theta[2]+rudder)); // Vertical movement

        float xNew = 0.5f*(xFront+xBack);
        float yNew = 0.5f*(yFront+yBack);

        // Accounting for discontinuities in theta...
        float ithetaNew = Mathf.Floor((theta[2]+Mathf.Pi)/(2*Mathf.Pi));
        float fthetaNew = Mathf.Atan2(xFront-xBack,-yFront+yBack);
        if (theta[2]-2*Mathf.Pi*ithetaNew < -Mathf.Pi/2 && fthetaNew >= Mathf.Pi/2)
            ithetaNew--;
        else if (theta[2]-2*Mathf.Pi*ithetaNew >= Mathf.Pi/2 && fthetaNew < -Mathf.Pi/2)
            ithetaNew++;
        float thetaNew = 2*Mathf.Pi*ithetaNew+fthetaNew;

        // FIXME: Use of timestamp[2]+delta here could be shaky if sending/receiving own position?
        long timestampNew = timestamp[2]+(long)(Mathf.Pow(10,7)*delta);

        // Set the player's new position (this derivation will always be true in resolving disputes)
        UpdatePosition(xNew,yNew,thetaNew,timestampNew);

        // No need to update quadratic model, as we will never have to predict our own position!
    }

    // Prediction
    public void UpdatePredictionModel(long interpolationTimestamp) // No inputs, as updating from the submarine's logged positions
    {
        //interpolationTimestamp = DateTime.UtcNow.Ticks; 
        //GD.Print(newTimestamp+" vs. "+INTERPOLATION_TIMESTAMP);

        //long sum = INTERPOLATION_TIMESTAMP+(long)(Mathf.Pow(10,7)*T_INTERPOLATION);
        //GD.Print(sum);

        //GD.Print(newTimestamp - INTERPOLATION_TIMESTAMP);
        //GD.Print((float)(newTimestamp-sum)*Mathf.Pow(10,-7));

        // Timestamps converted to seconds
        float[] deltas = new float[2] { Mathf.Pow(10, -7) * (timestamp[1] - timestamp[0]), Mathf.Pow(10, -7) * (timestamp[2] - timestamp[1]) };

        // Averaging velocities and accelerations
        float[] ux = new float[2] { (x[1] - x[0]) / deltas[0], (x[2] - x[1]) / deltas[1] };
        float[] ax = new float[1] { (ux[1] - ux[0]) / deltas[0] };

        float[] uy = new float[2] { (y[1] - y[0]) / deltas[0], (y[2] - y[1]) / deltas[1] };
        float[] ay = new float[1] { (uy[1] - uy[0]) / deltas[0] };

        float[] utheta = new float[2] { (theta[1] - theta[0]) / deltas[0], (theta[2] - theta[1]) / deltas[1] };
        float[] atheta = new float[1] { (utheta[1] - utheta[0]) / deltas[0] };

        // 'Catch up' back-end 
        if (positionInitialised)
        {
            //if (true)//interpolationTimestamp >= INTERPOLATION_TIMESTAMP+(long)(Mathf.Pow(10,7)*T_INTERPOLATION)) // CASE: Previous interpolation has finished
            //{
            X[0] = X[1];
            Y[0] = Y[1];
            THETA[0] = THETA[1];
            TIMESTAMP[0] = TIMESTAMP[1];
            //}
            /*else // CASE: Mid-way through previous interpolation; we 'stop where we are' as backPrediction...
            {
                (float x, float y, float theta) interpolation = InterpolatePosition(interpolationTimestamp);

                X[0] = new float[3] { interpolation.x, 0.0f, 0.0f };
                Y[0] = new float[3] { interpolation.y, 0.0f, 0.0f };
                THETA[0] = new float[2] { interpolation.theta, 0.0f }; 
                TIMESTAMP[0] = interpolationTimestamp;

                // DEBUG:
                //X[1] = new float[3] { x[2], ux[1], ax[0] };
                //Y[1] = new float[3] { y[2], uy[1], ay[0] };
                //THETA[1] = new float[2] { theta[1], utheta[0] }; // NB: Left linear, since rudder moves 'zero to sixty'!
                //TIMESTAMP[1] = timestamp[2];

                GD.Print("Catching up submarine from client "+captain.clientID+"!");
                GD.Print(((float)(interpolationTimestamp-INTERPOLATION_TIMESTAMP)*Mathf.Pow(10,-7))/T_INTERPOLATION);
            }*/

            // Update parameters of quadratic model
            X[1] = new float[3] { x[2], ux[1], ax[0] };
            Y[1] = new float[3] { y[2], uy[1], ay[0] };
            THETA[1] = new float[2] { theta[1], utheta[0] }; // NB: Left linear, since rudder moves 'zero to sixty'!
            TIMESTAMP[1] = timestamp[2];

            // DEBUG:
            //X[0] = X[1];
            //Y[0] = Y[1];
            //THETA[0] = THETA[1];
            //TIMESTAMP[0] = TIMESTAMP[1];
        }
        else
        {
            // Update parameters of quadratic model
            X = new float[2][];
            Y = new float[2][];
            THETA = new float[2][];
            TIMESTAMP = new long[2];

            X[1] = new float[3] { x[2], ux[1], ax[0] };
            Y[1] = new float[3] { y[2], uy[1], ay[0] };
            THETA[1] = new float[2] { theta[1], utheta[0] }; // NB: Left linear, since rudder moves 'zero to sixty'!
            TIMESTAMP[1] = timestamp[2];

            X[0] = X[1];
            Y[0] = Y[1];
            THETA[0] = THETA[1];
            TIMESTAMP[0] = TIMESTAMP[1];
        }

        INTERPOLATION_TIMESTAMP = interpolationTimestamp;
    }

    public (float xPrediction, float yPrediction, float thetaPrediction) QuadraticPredictPosition(long timestampPrediction, int index)
    {
        // Don't bother predicting the first couple of moves from rest... // FXIME: Could not predict move 0, linearly predict move 1, then use quadratic, but it's only a few (barely perceptible!) tenths of a second
        if ((x[0] == stationaryX && y[0] == stationaryY && theta[0] == stationaryTheta) || (x[1] == x[0] && y[1] == y[0] && theta[1] == theta[0])) // Second clause covers for lack of stationaryVariables integration elsewhere!
            return (xPrediction: X[index][0], yPrediction: Y[index][0], thetaPrediction: THETA[index][0]);

        // Derive delta from timestamps
        float t = Mathf.Pow(10, -7) * (timestampPrediction - TIMESTAMP[index]);

        // Quadratically predict positions at time t
        float xPrediction = X[index][0] + X[index][1] * t + 0.5f * X[index][2] * t * t;
        float yPrediction = Y[index][0] + Y[index][1] * t + 0.5f * Y[index][2] * t * t;
        float thetaPrediction = THETA[index][0] + THETA[index][1] * t;

        return (xPrediction: xPrediction, yPrediction: yPrediction, thetaPrediction: thetaPrediction);
    }

    public (float xInterpolation, float yInterpolation, float thetaInterpolation) InterpolatePosition(long timestampPrediction)
    {
        // DEBUG:
        return (xInterpolation: x[2], yInterpolation: y[2], thetaInterpolation: theta[2]);

        // DEBUG:
        return QuadraticPredictPosition(timestampPrediction,1);

        (float x, float y, float theta) frontPrediction = QuadraticPredictPosition(timestampPrediction,1);
        (float x, float y, float theta) backPrediction = QuadraticPredictPosition(timestampPrediction,0);

        float t = Mathf.Pow(10, -7) * (timestampPrediction - INTERPOLATION_TIMESTAMP); // NB: T seconds after frontPrediction was updated, we must be completely on that trajectory!
        t = Mathf.Clamp(t,0.0f,T_INTERPOLATION)/T_INTERPOLATION;

        return (xInterpolation: (1.0f-t)*backPrediction.x+t*frontPrediction.x, yInterpolation: (1.0f-t)*backPrediction.y+t*frontPrediction.y, thetaInterpolation: (1.0f-t)*backPrediction.theta+t*frontPrediction.theta);
    }
}

public class Crew
{
    public int clientID;
    public string clientIP;

    public Crew(int init_clientID, string init_clientIP)
    {
        clientID = init_clientID;
        clientIP = init_clientIP;
    }
}
