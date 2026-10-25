using System;
using UnityEngine;

// Runtime presentation for the prototype battlefield: green grass and a
// central river. The river is visual only, so it cannot obstruct physics.
public class BattlefieldEnvironment : MonoBehaviour
{
    public float riverWidth = 24f;
    public float riverLength = 300f;

    public void Build()
    {
        ColorGround();
        CreateRiver();
    }

    void ColorGround()
    {
        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!renderer.gameObject.name.Equals("Plane", StringComparison.OrdinalIgnoreCase))
                continue;
            Material material = new Material(renderer.sharedMaterial);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.20f, 0.48f, 0.16f));
            else material.color = new Color(0.20f, 0.48f, 0.16f);
            renderer.material = material;
        }
    }

    void CreateRiver()
    {
        if (GameObject.Find("Central River") != null)
            return;

        FindBaseAxis(out Vector3 center, out Vector3 teamAxis);
        GameObject river = GameObject.CreatePrimitive(PrimitiveType.Cube);
        river.name = "Central River";
        river.transform.position = center + Vector3.up * 0.03f;
        river.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, teamAxis), Vector3.up);
        river.transform.localScale = new Vector3(riverWidth, 0.06f, riverLength);
        Collider collider = river.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        Renderer renderer = river.GetComponent<Renderer>();
        Material water = new Material(Shader.Find("Sprites/Default"));
        water.color = new Color(0.04f, 0.28f, 0.62f, 0.9f);
        renderer.material = water;
    }

    void FindBaseAxis(out Vector3 center, out Vector3 axis)
    {
        TeamMember a = null, b = null;
        foreach (TeamMember member in FindObjectsByType<TeamMember>(FindObjectsSortMode.None))
        {
            if (member.gameObject.name.IndexOf("MainBase", StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (member.team == Team.TeamA) a = member;
            if (member.team == Team.TeamB) b = member;
        }
        center = a != null && b != null ? (a.transform.position + b.transform.position) * 0.5f : Vector3.zero;
        axis = a != null && b != null ? (a.transform.position - b.transform.position).normalized : Vector3.right;
        axis.y = 0f;
        if (axis.sqrMagnitude < 0.01f) axis = Vector3.right;
        else axis.Normalize();
    }
}
