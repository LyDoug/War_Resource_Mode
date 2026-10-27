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

        Vector3 offset = navigation.target.position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude > attackRange * attackRange)
            return;

        // Combat targets can be either an enemy unit or an objective. This
        // lets the commander react to an exposed player vehicle before it
        // resumes its strategic push against cities/main base.
        Health vehicleTarget = navigation.target.GetComponentInParent<Health>() ??
            navigation.target.GetComponentInChildren<Health>();
        if (vehicleTarget != null)
        {
            vehicleTarget.TakeDamage(damagePerShot);
            nextAttack = Time.time + attackInterval;
            return;
        }

        CaptureTriggerAdvanced capture = navigation.target.GetComponent<CaptureTriggerAdvanced>();
        BaseDestructibleWithHealthVisual target = capture != null
            ? capture.baseScript ?? capture.GetComponentInParent<BaseDestructibleWithHealthVisual>()
            : navigation.target.GetComponentInParent<BaseDestructibleWithHealthVisual>();
        if (target == null)
            return;

        target.TakeDamage(damagePerShot);
        nextAttack = Time.time + attackInterval;
    }
}
