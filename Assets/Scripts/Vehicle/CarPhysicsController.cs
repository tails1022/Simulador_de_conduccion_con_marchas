using System;
using UnityEngine;
using UnityEngine.InputSystem;
using GyroPilot.Networking;

namespace GyroPilot.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GearboxSystem))]
    public class CarPhysicsController : MonoBehaviour
    {
        [Header("Especificaciones del Vehculo")]
        public string vehicleName = "Apex GT";
        public float engineHorsepower = 480f;     // Potencia de aceleracin
        public float brakeForce = 5500f;          // Potencia de frenado (mejorada y contundente)
        public float maxSteerAngle = 35f;         // ngulo mximo de giro de ruedas delanteras
        public float downforceConstant = 15f;     // Carga aerodinmica
        public float topSpeedKmh = 315f;

        [Header("Ajustes de Dirección")]
        [Tooltip("Invierte la dirección del volante si gira al revés.")]
        public bool invertSteering = true;

        [Header("Encendido / Apagado de Motor")]
        public bool isEngineRunning = false;

        public void ToggleEngine()
        {
            isEngineRunning = !isEngineRunning;
            if (Gearbox != null)
            {
                Gearbox.isEngineRunning = isEngineRunning;
            }
        }

        private int lastNetEngineState = -1;

        [Header("Geometra y Volante Interior")]
        public Transform steeringWheel3D;         // Volante 3D dentro de la cabina
        public Transform cockpitCameraSeat;       // Posicin de la cabeza del piloto
        public Transform[] frontWheels;
        public Transform[] rearWheels;

        [Header("Telemetra en Vivo")]
        public float currentSpeedKmh;
        public float currentSteerInput;
        public float currentThrottleInput;
        public float currentBrakeInput;

        public Rigidbody CarRigidbody { get; private set; }
        public GearboxSystem Gearbox { get; private set; }

        private float currentSteerAngle = 0f;
        private Quaternion initialSteeringWheelRot = Quaternion.identity;
        private AudioSource engineAudio;

        private void Awake()
        {
            CarRigidbody = GetComponent<Rigidbody>();
            Gearbox = GetComponent<GearboxSystem>();

            // Centro de masa bajo para evitar que el auto vuelque
            CarRigidbody.centerOfMass = new Vector3(0f, -0.4f, 0.1f);
            CarRigidbody.mass = 1250f;
            CarRigidbody.linearDamping = 0.04f; // Inercia calibrada
            CarRigidbody.angularDamping = 2.0f;

            // Material sin fricción para el chasis: evita que el BoxCollider raspe/frene contra la pista con el Downforce
            // Toda la tracción, resistencia aerodinámica y agarre lateral se calculan fielmente en este script
            PhysicsMaterial frictionlessMat = new PhysicsMaterial("CarChassisPhysicsMat")
            {
                dynamicFriction = 0.0f,
                staticFriction = 0.0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0.0f,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                col.material = frictionlessMat;
            }

            invertSteering = GyroPilot.UI.SettingsManager.GetInvertSteering(invertSteering);
            isEngineRunning = false;
            if (Gearbox != null) Gearbox.isEngineRunning = false;

            // Auto-detectar volante o asiento si tienen nombres comunes
            if (steeringWheel3D == null)
            {
                steeringWheel3D = transform.Find("SteeringWheel") ?? transform.Find("Volante") ?? transform.Find("Steering_Wheel");
            }

            if (steeringWheel3D != null)
            {
                initialSteeringWheelRot = steeringWheel3D.localRotation;
            }

            if (cockpitCameraSeat == null)
            {
                cockpitCameraSeat = transform.Find("CockpitSeat") ?? transform.Find("Seat") ?? transform.Find("CameraSeat") ?? transform.Find("DriverSeat");
            }

            // Verificar si el carro tiene collider
            if (GetComponentInChildren<Collider>() == null)
            {
                Debug.LogWarning($"[CarPhysicsController] AVISO: '{gameObject.name}' no tiene ningn Collider. Agrega un BoxCollider o MeshCollider para que no caiga al vaco.");
            }

            SetupEngineAudio();
        }

        private void SetupEngineAudio()
        {
            engineAudio = gameObject.AddComponent<AudioSource>();
            engineAudio.loop = true;
            engineAudio.playOnAwake = false;
            engineAudio.spatialBlend = 0.2f; // Mayormente estreo en primera persona
            engineAudio.volume = 0.35f;

            // Generador de tono de motor sintetizado si no hay clip asignado
            engineAudio.clip = CreateEngineAudioClip();
            engineAudio.Play();
        }

        private AudioClip CreateEngineAudioClip()
        {
            int sampleRate = 44100;
            float duration = 1.0f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Combinacin armnica que simula un motor V6/V8
                float wave = Mathf.Sin(2 * Mathf.PI * 65f * t) * 0.45f +
                             Mathf.Sin(2 * Mathf.PI * 130f * t) * 0.35f +
                             Mathf.Sin(2 * Mathf.PI * 260f * t) * 0.2f;
                samples[i] = wave;
            }

            AudioClip clip = AudioClip.Create("SynthesizedEngine", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void Update()
        {
            GatherInput();
            UpdateVisuals();
            UpdateAudio();
        }

        private void FixedUpdate()
        {
            ApplyPhysics();
        }

        private void GatherInput()
        {
            float steer = 0f;
            float throttle = 0f;
            float brake = 0f;
            int targetGear = Gearbox.CurrentGear;

            // 1. Recibir datos del celular si el servidor está activo y conectado
            if (TelemetryServer.Instance != null && TelemetryServer.Instance.IsClientConnected)
            {
                VehicleInputData netInput = TelemetryServer.Instance.GetLatestInput();
                steer = netInput.steer;
                throttle = netInput.throttle;
                brake = netInput.brake;
                targetGear = netInput.gear;

                if (lastNetEngineState != -1 && netInput.engine != lastNetEngineState)
                {
                    isEngineRunning = (netInput.engine == 1);
                    if (Gearbox != null) Gearbox.isEngineRunning = isEngineRunning;
                }
                lastNetEngineState = netInput.engine;
            }

            // 2. Respaldo simultneo con Nuevo Input System (Teclado)
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // Giro (A / D o Flechas Izq / Der)
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steer = -1f;
                else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steer = 1f;

                // Aceleracin (W o Flecha Arriba)
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) throttle = 1f;

                // Freno (S, Flecha Abajo o Espacio)
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed || keyboard.spaceKey.isPressed) brake = 1f;

                // Marchas directas
                if (keyboard.rKey.wasPressedThisFrame) targetGear = -1;
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) targetGear = 1;
                if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) targetGear = 2;
                if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) targetGear = 3;
                if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) targetGear = 4;
                if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) targetGear = 5;

                // Encender / Apagar motor
                if (keyboard.eKey.wasPressedThisFrame)
                {
                    ToggleEngine();
                }
            }

            if (!isEngineRunning)
            {
                throttle = 0f;
            }

            // 3. Soporte opcional para Gamepad / Mando de PC
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                float stickSteer = gamepad.leftStick.x.ReadValue();
                if (Mathf.Abs(stickSteer) > 0.1f) steer = stickSteer;

                float rightTrigger = gamepad.rightTrigger.ReadValue();
                if (rightTrigger > 0.05f) throttle = Mathf.Max(throttle, rightTrigger);

                float leftTrigger = gamepad.leftTrigger.ReadValue();
                if (leftTrigger > 0.05f) brake = Mathf.Max(brake, leftTrigger);
            }

            if (invertSteering)
            {
                steer = -steer;
            }

            currentSteerInput = Mathf.Clamp(steer, -1f, 1f);

            // Entrada de acelerador progresiva: acelera de forma suave y dosificada sin tirones bruscos
            float targetThrottle = Mathf.Clamp01(throttle);
            float throttleSpeed = (targetThrottle > currentThrottleInput) ? 2.8f : 8.0f;
            currentThrottleInput = Mathf.MoveTowards(currentThrottleInput, targetThrottle, Time.deltaTime * throttleSpeed);

            currentBrakeInput = Mathf.Clamp01(brake);

            // Solicitar cambio a la transmisin
            if (targetGear != Gearbox.CurrentGear)
            {
                Gearbox.RequestGearChange(targetGear, currentThrottleInput);
            }
        }

        private void ApplyPhysics()
        {
            // Clculo de velocidad en km/h
            Vector3 forward = transform.forward;
            float forwardSpeed = Vector3.Dot(CarRigidbody.linearVelocity, forward);
            currentSpeedKmh = forwardSpeed * 3.6f;

            // Sensibilidad de direccin dependiente de la velocidad
            float speedFactor = Mathf.Clamp01(1.0f - (Mathf.Abs(currentSpeedKmh) / (topSpeedKmh * 1.2f)));
            float targetAngle = currentSteerInput * maxSteerAngle * Mathf.Lerp(0.5f, 1.0f, speedFactor);
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, Time.fixedDeltaTime * 12f);

            // 1. Aceleración por marcha
            GearRatio ratio = Gearbox.GetGearRatio(Gearbox.CurrentGear);
            float effectiveThrottle = Gearbox.GetEffectiveThrottle(currentThrottleInput);
            if (!isEngineRunning) effectiveThrottle = 0f;

            if (effectiveThrottle > 0.01f && Gearbox.CurrentGear != 0)
            {
                float direction = Gearbox.CurrentGear == -1 ? -1f : 1f;
                float currentSpeed = Mathf.Abs(currentSpeedKmh);

                // Corte estricto de inyección / Rev Limiter de cada marcha
                // El auto no puede rebasar la velocidad máxima de la marcha actual con el acelerador
                float revLimiterFactor = 1.0f;
                if (currentSpeed >= ratio.maxSpeedKmh)
                {
                    // Corte absoluto de tracción en la velocidad tope de la marcha
                    revLimiterFactor = 0.0f;
                }
                else if (currentSpeed > ratio.maxSpeedKmh * 0.92f)
                {
                    // Curva progresiva que corta a cero en el tope
                    float t = (currentSpeed - ratio.maxSpeedKmh * 0.92f) / (ratio.maxSpeedKmh * 0.08f);
                    revLimiterFactor = Mathf.Cos(t * Mathf.PI * 0.5f);
                }

                // Penalización por marcha alta a baja velocidad (Ahogamiento de motor / Clutch Stall)
                float luggingFactor = Gearbox.GetLuggingFactor(currentSpeed);

                // Curva de empuje del motor por RPM: entrega potencia sólida y constante
                float rpmRatio = Mathf.Clamp01((Gearbox.CurrentRPM - Gearbox.IdleRPM) / (Gearbox.MaxRPM - Gearbox.IdleRPM));
                float powerCurve = Mathf.Lerp(0.92f, 1.12f, Mathf.Sin(rpmRatio * Mathf.PI * 0.85f));

                float driveForce = engineHorsepower * 22f * ratio.torqueFactor * luggingFactor * effectiveThrottle * powerCurve * revLimiterFactor;
                CarRigidbody.AddForce(forward * (driveForce * direction), ForceMode.Force);
            }

            // Freno de motor (Engine Braking) proporcional a la reducción de marcha y a las altas RPM mecánicas
            if (effectiveThrottle <= 0.05f && Gearbox.CurrentGear != 0 && Mathf.Abs(forwardSpeed) > 0.3f)
            {
                float rpmBrakeFactor = Mathf.Clamp01((Gearbox.CurrentRPM - Gearbox.IdleRPM) / (Gearbox.RedlineRPM - Gearbox.IdleRPM));
                // Retención contundente en reducciones: en 1ª o 2ª a alta velocidad retiene con fuerza (~5500 N)
                float baseEngineBrake = 1800f * ratio.torqueFactor;
                float downshiftBrake = 3400f * (ratio.torqueFactor * ratio.torqueFactor) * rpmBrakeFactor;
                float totalEngineBrake = baseEngineBrake + downshiftBrake;

                CarRigidbody.AddForce(-forward * (totalEngineBrake * Mathf.Sign(forwardSpeed)), ForceMode.Force);
            }

            // Resistencia natural de rodadura de los neumáticos
            if (Mathf.Abs(forwardSpeed) > 0.2f)
            {
                CarRigidbody.AddForce(-forward * (300f * Mathf.Sign(forwardSpeed)), ForceMode.Force);
            }

            // Resistencia aerodinámica del viento (frena naturalmente a altas velocidades sin ahogar la aceleración)
            float airDrag = 0.28f * (forwardSpeed * forwardSpeed) * Mathf.Sign(forwardSpeed);
            CarRigidbody.AddForce(-forward * airDrag, ForceMode.Force);

            // 2. Frenado de servicio (potente y progresivo)
            if (currentBrakeInput > 0.01f && CarRigidbody.linearVelocity.magnitude > 0.1f)
            {
                Vector3 brakeDir = -CarRigidbody.linearVelocity.normalized;
                CarRigidbody.AddForce(brakeDir * (brakeForce * currentBrakeInput), ForceMode.Force);
            }

            // 3. Friccin lateral (evita derrapes descontrolados)
            Vector3 right = transform.right;
            float lateralSpeed = Vector3.Dot(CarRigidbody.linearVelocity, right);
            float lateralFriction = -lateralSpeed * 40f;
            CarRigidbody.AddForce(right * lateralFriction, ForceMode.Acceleration);

            // 4. Giro del automvil (MoveRotation garantiza giro gil y suave)
            if (Mathf.Abs(forwardSpeed) > 0.15f)
            {
                float turnDir = Mathf.Sign(forwardSpeed);
                float turnRate = currentSteerAngle * 2.2f * turnDir;
                Quaternion deltaRot = Quaternion.Euler(0f, turnRate * Time.fixedDeltaTime, 0f);
                CarRigidbody.MoveRotation(CarRigidbody.rotation * deltaRot);
            }

            // 5. Downforce (Carga aerodinmica que pega el auto a la pista)
            float downforce = downforceConstant * (forwardSpeed * forwardSpeed) * 0.05f;
            CarRigidbody.AddForce(-Vector3.up * downforce, ForceMode.Force);

            // Actualizar RPM en la transmisin
            Gearbox.UpdateEngineRPM(currentSpeedKmh, currentThrottleInput);
        }

        private void UpdateVisuals()
        {
            // Rotar el volante 3D dentro del habitculo respetando su inclinacin original
            if (steeringWheel3D != null)
            {
                float wheelRotation = currentSteerInput * 90f;
                steeringWheel3D.localRotation = initialSteeringWheelRot * Quaternion.Euler(0f, 0f, -wheelRotation);
            }

            // Rotar ruedas delanteras visualmente
            if (frontWheels != null)
            {
                foreach (var wheel in frontWheels)
                {
                    if (wheel != null)
                    {
                        wheel.localRotation = Quaternion.Euler(0f, currentSteerAngle, 0f);
                    }
                }
            }
        }

        private void UpdateAudio()
        {
            if (engineAudio == null) return;

            if (!isEngineRunning)
            {
                engineAudio.volume = Mathf.Lerp(engineAudio.volume, 0f, Time.deltaTime * 6f);
                engineAudio.pitch = Mathf.Lerp(engineAudio.pitch, 0.4f, Time.deltaTime * 6f);
                return;
            }

            // Pitch del audio ajustado a las RPM (0.75 a 2.5)
            float rpmRatio = Mathf.Clamp01((Gearbox.CurrentRPM - Gearbox.IdleRPM) / (Gearbox.MaxRPM - Gearbox.IdleRPM));
            float pitch = Mathf.Lerp(0.75f, 2.5f, rpmRatio);

            if (Gearbox != null && Gearbox.IsEngineLugging)
            {
                // Sonido áspero y ruidoso cuando el motor se está ahogando
                pitch += Mathf.Sin(Time.time * 50f) * 0.05f;
            }
            else if (Gearbox != null && Gearbox.IsHittingRevLimiter)
            {
                // Sonido de corte de encendido (corte deportivo al límite de marcha)
                pitch += Mathf.Sin(Time.time * 50f) * 0.08f;
            }

            engineAudio.pitch = Mathf.Clamp(pitch, 0.5f, 2.6f);
            
            // Volumen dinámico: se escucha potente tanto acelerando como reteniendo con freno de motor a altas RPM
            float throttleOrOverrun = Mathf.Max(currentThrottleInput, rpmRatio * 0.70f);
            float vol = Mathf.Lerp(0.25f, 0.55f, throttleOrOverrun);
            if (Gearbox != null && Gearbox.IsHittingRevLimiter)
            {
                vol *= (Mathf.Sin(Time.time * 50f) > 0f) ? 1.0f : 0.70f;
            }
            engineAudio.volume = vol;
        }
    }
}
