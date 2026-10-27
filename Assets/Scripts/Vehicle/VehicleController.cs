using UnityEngine;

public class VehicleController : MonoBehaviour
{
    MonoBehaviour[] scripts;

    // Authoritative marker for the unit currently driven by the local player.
    public bool IsControlled { get; private set; }

    public static bool IsLocallyControlled(Component component)
    {
        if (component == null)
            return false;

        // A few legacy prefabs contain a duplicate VehicleController on their
        // root. The vehicle is player-driven when any controller is active.
        // This keeps those prefabs from accidentally rejecting all input.
        foreach (VehicleController controller in
                 component.transform.root.GetComponentsInChildren<VehicleController>(true))
        {
            if (controller.IsControlled)
                return true;
        }
        return false;
    }

    void Awake()
    {
        scripts = GetComponentsInChildren<MonoBehaviour>();
    }

    public void SetControlled(bool value)
    {
        IsControlled = value;

        foreach (MonoBehaviour s in scripts)
        {
            // não desliga este script
            if (s == this)
                continue;

            s.enabled = value;
        }

        Rigidbody[] rbs =
            GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rbs)
        {
            rb.isKinematic = !value;
        }

        Debug.Log(
            value
            ? "🎮 Veículo ATIVO"
            : "💤 Veículo DESATIVADO"
        );
    }
}
