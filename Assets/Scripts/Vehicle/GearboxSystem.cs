using System;
using UnityEngine;

namespace GyroPilot.Vehicle
{
    [System.Serializable]
    public class GearRatio
    {
        public int gearNumber;       // -1 = R, 0 = N, 1..5
        public string gearName;      // "R", "N", "1", etc.
        public float maxSpeedKmh;    // Velocidad tope en esta marcha
        public float torqueFactor;   // Multiplicador de fuerza de aceleracin
    }

    public class GearboxSystem : MonoBehaviour
    {
        [Header("Configuración de Marchas")]
        public GearRatio[] gears = new GearRatio[]
        {
            new GearRatio { gearNumber = -1, gearName = "R", maxSpeedKmh = 50f,  torqueFactor = 1.15f },
            new GearRatio { gearNumber = 0,  gearName = "N", maxSpeedKmh = 0f,   torqueFactor = 0.00f },
            new GearRatio { gearNumber = 1,  gearName = "1", maxSpeedKmh = 68f,  torqueFactor = 1.20f },
            new GearRatio { gearNumber = 2,  gearName = "2", maxSpeedKmh = 120f, torqueFactor = 1.00f },
            new GearRatio { gearNumber = 3,  gearName = "3", maxSpeedKmh = 175f, torqueFactor = 0.85f },
            new GearRatio { gearNumber = 4,  gearName = "4", maxSpeedKmh = 235f, torqueFactor = 0.75f },
            new GearRatio { gearNumber = 5,  gearName = "5", maxSpeedKmh = 310f, torqueFactor = 0.70f },
        };

        [Header("Estado de la Transmisión")]
        public int CurrentGear { get; private set; } = 1;
        public float CurrentRPM { get; private set; } = 0f;
        public float IdleRPM = 900f;
        public float MaxRPM = 8200f;
        public float RedlineRPM = 7500f;
        public bool IsHittingRevLimiter { get; private set; } = false;

        [Header("Embrague Semiautomático de Competición")]
        public bool IsIgnitionCut { get; private set; } = false;
        public bool ShowClutchWarning { get; private set; } = false;
        public string ClutchWarningMessage { get; private set; } = "";

        [Header("Encendido de Motor")]
        public bool isEngineRunning = false;

        [Header("Ahogamiento de Motor (Lugging por marcha alta a baja velocidad)")]
        public bool IsEngineLugging { get; private set; } = false;
        public string LuggingWarningMessage { get; private set; } = "";

        private float ignitionCutTimer = 0f;
        private float warningTimer = 0f;
        private const float IGNITION_CUT_DURATION = 0.15f; // 150ms corte de gas
        private int pendingGear = 1;

        public event Action<int> OnGearChanged;

        private void Awake()
        {
            // Garantizar ratios óptimos, suaves y equilibrados de potencia
            GearRatio g1 = GetGearRatio(1);
            if (g1 != null) g1.torqueFactor = 1.20f;
            GearRatio g2 = GetGearRatio(2);
            if (g2 != null) g2.torqueFactor = 1.00f;
            GearRatio g3 = GetGearRatio(3);
            if (g3 != null) g3.torqueFactor = 0.85f;
            GearRatio g4 = GetGearRatio(4);
            if (g4 != null) { g4.torqueFactor = 0.75f; g4.maxSpeedKmh = Mathf.Max(g4.maxSpeedKmh, 235f); }
            GearRatio g5 = GetGearRatio(5);
            if (g5 != null) { g5.torqueFactor = 0.70f; g5.maxSpeedKmh = Mathf.Max(g5.maxSpeedKmh, 310f); }
        }

        public void RequestGearChange(int targetGear, float currentThrottle)
        {
            if (targetGear == CurrentGear) return;
            if (targetGear < -1 || targetGear > 5) return;

            // Si el acelerador est presionado (> 10%), aplicar corte de inyeccin de 150ms y advertencia visual
            if (currentThrottle > 0.10f)
            {
                pendingGear = targetGear;
                IsIgnitionCut = true;
                ignitionCutTimer = IGNITION_CUT_DURATION;

                ShowClutchWarning = true;
                warningTimer = 1.3f;
                ClutchWarningMessage = "SUELTA ACELERADOR PARA ENGRANAR!";
            }
            else
            {
                // Cambio suave inmediato si no se acelera
                ExecuteGearChange(targetGear);
            }
        }

        private void Update()
        {
            // Manejo del corte de inyeccin (150ms)
            if (IsIgnitionCut)
            {
                ignitionCutTimer -= Time.deltaTime;
                if (ignitionCutTimer <= 0f)
                {
                    IsIgnitionCut = false;
                    ExecuteGearChange(pendingGear);
                }
            }

            // Temporizador de advertencia visual
            if (ShowClutchWarning)
            {
                warningTimer -= Time.deltaTime;
                if (warningTimer <= 0f)
                {
                    ShowClutchWarning = false;
                }
            }
        }

        private float lastCarSpeedKmh = 0f;

        private void ExecuteGearChange(int newGear)
        {
            CurrentGear = newGear;

            // Al cambiar de marcha, la aguja se ajusta de inmediato al ratio mecánico de la nueva marcha
            if (newGear != 0)
            {
                GearRatio newRatio = GetGearRatio(newGear);
                float speedRatio = Mathf.Clamp01(Mathf.Abs(lastCarSpeedKmh) / Mathf.Max(newRatio.maxSpeedKmh, 10f));
                // Si reducimos a una marcha más baja a alta velocidad, la aguja salta de inmediato a las altas RPM reales
                CurrentRPM = Mathf.Lerp(IdleRPM, RedlineRPM + 150f, speedRatio);
            }

            OnGearChanged?.Invoke(CurrentGear);
        }

        public void UpdateEngineRPM(float speedKmh, float throttle)
        {
            lastCarSpeedKmh = speedKmh;

            if (!isEngineRunning)
            {
                IsEngineLugging = false;
                CurrentRPM = Mathf.Lerp(CurrentRPM, 0f, Time.deltaTime * 10f);
                return;
            }

            GearRatio ratio = GetGearRatio(CurrentGear);
            float absSpeed = Mathf.Abs(speedKmh);

            if (CurrentGear == 0)
            {
                IsEngineLugging = false;
                // Neutral: responde al acelerador de forma progresiva y cae ágilmente
                float targetRpm = Mathf.Lerp(IdleRPM, MaxRPM * 0.85f, throttle);
                float rate = (targetRpm < CurrentRPM) ? 22f : 12f;
                CurrentRPM = Mathf.Lerp(CurrentRPM, targetRpm, Time.deltaTime * rate);
            }
            else
            {
                // Detectar si el motor se ahoga al intentar acelerar en marcha alta a baja velocidad
                if (throttle > 0.12f && CurrentGear >= 3 && absSpeed < 35f)
                {
                    IsEngineLugging = true;
                    LuggingWarningMessage = "⚠️ ¡MOTOR AHOGADO! REDUCE A 1ª MARCHA";
                }
                else
                {
                    IsEngineLugging = false;
                }

                // Engranado: acople mecánico directo de la transmisión a las ruedas
                float speedRatio = Mathf.Clamp01(absSpeed / Mathf.Max(ratio.maxSpeedKmh, 10f));
                float mechanicalRpm = Mathf.Lerp(IdleRPM, RedlineRPM, speedRatio);

                // Detección y rebote en el corte de inyección (Rev Limiter en la velocidad límite de la marcha)
                if (absSpeed >= ratio.maxSpeedKmh * 0.96f && throttle > 0.1f)
                {
                    IsHittingRevLimiter = true;
                    // Rebote rápido en el corte (efecto tatatata en ~7500-7650 RPM)
                    float limiterBounce = Mathf.Sin(Time.time * 50f) * 120f;
                    mechanicalRpm = Mathf.Clamp(RedlineRPM + limiterBounce, RedlineRPM - 180f, RedlineRPM + 150f);
                }
                else
                {
                    IsHittingRevLimiter = false;
                }

                // Si el motor está ahogado, retener RPM cerca de ralentí con ligero temblor mecánico
                if (IsEngineLugging)
                {
                    float lugTremor = Mathf.Sin(Time.time * 42f) * 65f;
                    mechanicalRpm = Mathf.Clamp(IdleRPM + 220f + lugTremor, IdleRPM, 1550f);
                }

                // Desacople natural del embrague a muy baja velocidad para que no baje del ralentí
                if (speedRatio < 0.10f && throttle < 0.05f)
                {
                    mechanicalRpm = Mathf.Lerp(IdleRPM, mechanicalRpm, speedRatio * 10f);
                }

                // Techo estricto: nunca rebasar MaxRPM
                mechanicalRpm = Mathf.Clamp(mechanicalRpm, 0f, MaxRPM);

                // Dinámica de RPM:
                // En reducciones (downshift sin acelerador), salta y se sostiene arriba de inmediato (rate 25)
                // Al acelerar suavemente desde baja velocidad, sube con aplomo y progresividad (rate 6)
                // Al desacelerar con freno de motor, sigue fielmente la pérdida de velocidad de las ruedas (rate 14)
                float rate;
                if (mechanicalRpm > CurrentRPM)
                {
                    rate = (throttle > 0.1f) ? 6f : 25f;
                }
                else
                {
                    rate = 14f;
                }

                CurrentRPM = Mathf.Lerp(CurrentRPM, mechanicalRpm, Time.deltaTime * rate);
                CurrentRPM = Mathf.Clamp(CurrentRPM, 0f, MaxRPM);
            }
        }

        /// <summary>
        /// Factor de penalización por intentar arrancar en marchas altas a baja velocidad.
        /// En 1ª marcha: 100% de fuerza en todo momento.
        /// En 2ª marcha: fuerza reducida al 35% en parado (arranque lento y pesado).
        /// En marchas superiores: motor ahogado solo a muy baja velocidad, pero con fuerza completa a velocidad de carrera.
        /// </summary>
        public float GetLuggingFactor(float speedKmh)
        {
            float absSpeed = Mathf.Abs(speedKmh);
            float minHealthySpeed = 0f;
            float stallTorque = 1.0f;

            switch (CurrentGear)
            {
                case 2:
                    minHealthySpeed = 16f;
                    stallTorque = 0.35f; // En 2ª le cuesta salir pero avanza lento
                    break;
                case 3:
                    minHealthySpeed = 38f;
                    stallTorque = 0.12f; // En 3ª prácticamente no avanza desde cero
                    break;
                case 4:
                    minHealthySpeed = 60f;
                    stallTorque = 0.10f; // En 4ª ahogado si está parado
                    break;
                case 5:
                    minHealthySpeed = 80f;
                    stallTorque = 0.08f; // En 5ª sin respuesta desde parado
                    break;
            }

            if (minHealthySpeed > 0f && absSpeed < minHealthySpeed)
            {
                float t = absSpeed / minHealthySpeed;
                return Mathf.Lerp(stallTorque, 1.0f, t * t);
            }

            return 1.0f;
        }

        public GearRatio GetGearRatio(int gear)
        {
            for (int i = 0; i < gears.Length; i++)
            {
                if (gears[i].gearNumber == gear) return gears[i];
            }
            return gears[2]; // Default 1 marcha
        }

        public float GetEffectiveThrottle(float rawThrottle)
        {
            if (IsIgnitionCut) return 0f; // Corte de 150ms
            return rawThrottle;
        }
    }
}
