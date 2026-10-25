using UnityEngine;

// Briefly removes collisions from a just-created vehicle so it can settle
// outside the extraction zone instead of being launched by overlapping props.
public class VehicleSpawnGhost : MonoBehaviour
{
    public float solidAfter = 0.65f;
    Collider[] colliders;

    void Awake()
    {
        colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
            col.enabled = false;
    }

    void Start() => Invoke(nameof(MakeSolid), solidAfter);

    void MakeSolid()
    {
        foreach (Collider col in colliders)
            if (col != null) col.enabled = true;
        Destroy(this);
    }
}
