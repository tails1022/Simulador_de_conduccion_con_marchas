using System;
using UnityEngine;
using UnityEngine.InputSystem;
using GyroPilot.Cameras;

namespace GyroPilot.Vehicle
{
    public class CarSelectionManager : MonoBehaviour
    {
        public static CarSelectionManager Instance { get; private set; }

        [Header("Tus 4 Autos en la Escena")]
        [Tooltip("Arrastra aqu los GameObjects de tus 4 vehculos que tienen CarPhysicsController.")]
        public CarPhysicsController[] cars = new CarPhysicsController[4];

        [Header("Cmara en Primera Persona")]
        [Tooltip("Arrastra aqu la Cmara Principal con el script CockpitCamera.")]
        public CockpitCamera cockpitCamera;

        [Header("Estado")]
        public int currentCarIndex = 0;
        public CarPhysicsController ActiveCar
        {
            get
            {
                if (cars != null && currentCarIndex >= 0 && currentCarIndex < cars.Length && cars[currentCarIndex] != null)
                {
                    return cars[currentCarIndex];
                }
                return FindAnyObjectByType<CarPhysicsController>();
            }
        }

        public event Action<CarPhysicsController> OnCarSelected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            RefreshCars();
        }

        public void RefreshCars()
        {
            cars = FindObjectsByType<CarPhysicsController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            cockpitCamera = FindAnyObjectByType<CockpitCamera>();
            if (cars != null && cars.Length > 0)
            {
                currentCarIndex = Mathf.Clamp(currentCarIndex, 0, cars.Length - 1);
                SelectCar(currentCarIndex);
            }
        }

        private void Start()
        {
            RefreshCars();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.cKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame))
            {
                SelectNextCar();
            }
        }

        public void SelectCar(int index)
        {
            if (cars == null || cars.Length == 0) return;
            if (index < 0 || index >= cars.Length || cars[index] == null) return;

            currentCarIndex = index;

            // Activar solo el auto seleccionado y ocultar los dems
            for (int i = 0; i < cars.Length; i++)
            {
                if (cars[i] != null)
                {
                    bool isActive = (i == currentCarIndex);
                    cars[i].gameObject.SetActive(isActive);
                }
            }

            CarPhysicsController selected = cars[currentCarIndex];

            // Conectar cmara de cabina al auto activo
            if (cockpitCamera != null)
            {
                cockpitCamera.SetTargetCar(selected);
            }

            OnCarSelected?.Invoke(selected);
            Debug.Log($"[CarSelectionManager] Auto activo: {selected.vehicleName} ({selected.name})");
        }

        public void SelectNextCar()
        {
            if (cars == null || cars.Length <= 1) return;
            int next = (currentCarIndex + 1) % cars.Length;
            SelectCar(next);
        }

        public void SelectPreviousCar()
        {
            if (cars == null || cars.Length <= 1) return;
            int prev = (currentCarIndex - 1 + cars.Length) % cars.Length;
            SelectCar(prev);
        }
    }
}
