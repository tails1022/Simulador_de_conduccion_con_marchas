using UnityEngine;
using GyroPilot.Vehicle;

namespace GyroPilot.Cameras
{
    public class CockpitCamera : MonoBehaviour
    {
        [Header("Objetivo")]
        [Tooltip("Arrastra aqu el auto que deseas seguir.")]
        public CarPhysicsController targetCar;
        [Tooltip("Posicin de los ojos del piloto respecto al auto si no tienes un asiento asignado.")]
        public Vector3 seatOffset = new Vector3(-0.35f, 0.95f, 0.1f); // Lado izquierdo, altura de ojos

        [Header("Dinmica de Cabina (Fuerzas G)")]
        [SerializeField] private float tiltSensitivity = 0.5f;
        [SerializeField] private float accelerationPitch = 0.5f;
        [SerializeField] private float vibrationIntensity = 0.0012f;

        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        private Vector3 currentPosVelocity;
        private float currentPitch = 0f;
        private float currentRoll = 0f;

        private void Start()
        {
            initialLocalPos = seatOffset;
            initialLocalRot = Quaternion.identity;

            if (targetCar == null)
            {
                targetCar = FindAnyObjectByType<CarPhysicsController>();
            }

            if (targetCar != null)
            {
                SetTargetCar(targetCar);
            }
        }

        public void SetTargetCar(CarPhysicsController car)
        {
            targetCar = car;
            if (car != null)
            {
                if (car.cockpitCameraSeat != null)
                {
                    transform.SetParent(car.cockpitCameraSeat);
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                }
                else
                {
                    transform.SetParent(car.transform);
                    transform.localPosition = seatOffset;
                    transform.localRotation = Quaternion.identity;
                }
            }
        }

        private void LateUpdate()
        {
            if (targetCar == null)
            {
                var car = FindAnyObjectByType<CarPhysicsController>();
                if (car != null) SetTargetCar(car);
                return;
            }

            // Inercia suave de aceleracin y frenado (Pitch)
            float targetPitch = 0f;
            if (targetCar.currentThrottleInput > 0.05f) targetPitch = -accelerationPitch * targetCar.currentThrottleInput;
            if (targetCar.currentBrakeInput > 0.05f) targetPitch = accelerationPitch * 1.2f * targetCar.currentBrakeInput;

            currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * 8f);

            // Inclinacin lateral en curvas suave (Head roll)
            float targetRoll = -targetCar.currentSteerInput * 2.5f * tiltSensitivity;
            currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * 8f);

            // Vibración sutil de alta velocidad (muy atenuada y suave)
            float speedKmh = Mathf.Abs(targetCar.currentSpeedKmh);
            Vector3 vibration = Vector3.zero;
            if (speedKmh > 70f)
            {
                float shakeRatio = Mathf.Clamp01((speedKmh - 70f) / Mathf.Max(targetCar.topSpeedKmh - 70f, 10f));
                float shake = shakeRatio * vibrationIntensity;
                vibration = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 14f, 0f) - 0.5f) * shake,
                    (Mathf.PerlinNoise(0f, Time.time * 14f) - 0.5f) * shake,
                    0f
                );
            }
            else if (targetCar.Gearbox != null && targetCar.Gearbox.IsEngineLugging)
            {
                // Temblor del habitáculo cuando el motor cabecea por ahogamiento
                float lugShake = 0.0018f;
                vibration = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 28f, 0f) - 0.5f) * lugShake,
                    (Mathf.PerlinNoise(0f, Time.time * 28f) - 0.5f) * lugShake,
                    0f
                );
            }

            Vector3 targetLocalPos = initialLocalPos + vibration;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * 18f);
            transform.localRotation = Quaternion.Euler(currentPitch, 0f, currentRoll);
        }
    }
}
