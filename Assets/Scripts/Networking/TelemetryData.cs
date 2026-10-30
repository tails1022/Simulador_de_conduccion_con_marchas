using System;

namespace GyroPilot.Networking
{
    [System.Serializable]
    public struct VehicleInputData
    {
        public float steer;     // -1.0 (izq) a +1.0 (der)
        public float throttle;  // 0.0 a 1.0
        public float brake;     // 0.0 a 1.0
        public int gear;        // -1 (R), 0 (N), 1..5
        public int engine;      // 0 = apagado, 1 = encendido
        public long timestamp;
        public bool isConnected;
    }
}
