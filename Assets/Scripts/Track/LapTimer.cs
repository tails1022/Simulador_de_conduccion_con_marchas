using System;
using UnityEngine;

namespace GyroPilot.Track
{
    public class LapTimer : MonoBehaviour
    {
        public static LapTimer Instance { get; private set; }

        public float CurrentLapTime { get; private set; } = 0f;
        public float BestLapTime { get; private set; } = 0f;
        public float LastLapTime { get; private set; } = 0f;
        public int CurrentLap { get; private set; } = 1;
        public bool IsTiming { get; set; } = true;

        public event Action<int, float> OnLapCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (IsTiming)
            {
                CurrentLapTime += Time.deltaTime;
            }
        }

        public void CompleteLap()
        {
            if (CurrentLapTime < 5f) return; // Evitar disparos falsos consecutivos

            LastLapTime = CurrentLapTime;
            if (BestLapTime <= 0f || LastLapTime < BestLapTime)
            {
                BestLapTime = LastLapTime;
            }

            OnLapCompleted?.Invoke(CurrentLap, LastLapTime);
            CurrentLap++;
            CurrentLapTime = 0f;
        }

        public static string FormatTime(float timeInSeconds)
        {
            if (timeInSeconds <= 0f) return "--:--.---";
            TimeSpan t = TimeSpan.FromSeconds(timeInSeconds);
            return string.Format("{0:D2}:{1:D2}.{2:D3}", t.Minutes, t.Seconds, t.Milliseconds);
        }
    }
}
