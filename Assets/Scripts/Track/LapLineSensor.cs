using UnityEngine;

namespace GyroPilot.Track
{
    /// <summary>
    /// Coloca este componente en el BoxCollider (IsTrigger = true) de tu lnea de meta.
    /// Registrar el paso de cualquier auto con CarPhysicsController.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LapLineSensor : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<Vehicle.CarPhysicsController>() != null)
            {
                LapTimer.Instance?.CompleteLap();
            }
        }
    }
}
