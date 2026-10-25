using UnityEngine;

// Structure damage for AI vehicles. It intentionally does not read player
// input; the navigation target selected by AIStructureTarget is its target.
public class AICombatAttack : MonoBehaviour
{
    public float attackRange = 32f;
    public float attackInterval = 2.2f;
    public float damagePerShot = 40f;
    float nextAttack;

    void Update()
    {
        if (Time.time < nextAttack)
            return;

        VehicleSimpleAI navigation = GetComponent<VehicleSimpleAI>();
        if (navigation == null || navigation.target == null)
            return;

        CaptureTriggerAdvanced capture = navigation.target.GetComponent<CaptureTriggerAdvanced>();
        if (capture == null)
            return;

        BaseDestructibleWithHealthVisual target = capture.baseScript ??
            capture.GetComponentInParent<BaseDestructibleWithHealthVisual>();
        if (target == null)
            return;

        Vector3 offset = navigation.target.position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude > attackRange * attackRange)
            return;

        target.TakeDamage(damagePerShot);
        nextAttack = Time.time + attackInterval;
    }
}
