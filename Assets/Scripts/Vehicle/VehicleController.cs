using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public static string PlayerPrompt { get; private set; }
    const float EjectionHoldDuration = 3f;
    MonoBehaviour[] scripts;
    float ejectionHeldTime;

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

    void Update()
    {
        if (!IsControlled || GameManager.Instance == null || !GameManager.Instance.MatchStarted)
        {
            ejectionHeldTime = 0f;
            if (PlayerPrompt != null && PlayerPrompt.StartsWith("SEGURE [J]"))
                PlayerPrompt = null;
            return;
        }

        if (!Input.GetKey(KeyCode.J))
        {
            ejectionHeldTime = 0f;
            PlayerPrompt = null;
            return;
        }

        ejectionHeldTime += Time.unscaledDeltaTime;
        PlayerPrompt = "EJETANDO... " + Mathf.CeilToInt(Mathf.Max(0f, EjectionHoldDuration - ejectionHeldTime)) + " s";
        if (ejectionHeldTime < EjectionHoldDuration)
            return;

        PlayerPrompt = null;
        SetControlled(false);
        GameManager.Instance.EjectPlayerToMainBase();
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
