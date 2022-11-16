using System;

namespace server
{
    public class Submarine
    {
        // Constants
        private const float structure = 20.0f;
        public float Structure
        {
            get { return structure; }
        }

        // Player-controlled variables
        public float gas;
        public float brakes;
        public float steer;

        // Variables
        public float a;
        public float u;
        public float x, y;
        public float theta;

        public long t0;

        public Submarine()
        {

        }

        public Submarine(float init_gas, float init_brakes, float init_steer, float init_a, float init_u, float init_x, float init_y, float init_theta, long init_t0)
        {
            gas = init_gas;
            brakes = init_brakes;
            steer = init_steer;

            a = init_a;
            u = init_u;
            x = init_x;
            y = init_y;
            theta = init_theta;

            t0 = init_t0;
        }

        ~Submarine()
        {

        }
    }
}
