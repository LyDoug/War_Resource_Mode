using UnityEngine;

public enum Team
{
    Neutral,
    TeamA,
    TeamB
}

public class TeamMember : MonoBehaviour
{
    [Header("Team")]
    public Team team = Team.Neutral;

    [Header("Colors")]
    public Color neutralColor = Color.gray;
    public Color teamAColor = Color.blue;
    public Color teamBColor = Color.red;

    [Header("Renderers (optional)")]
    public Renderer[] renderers;
    MaterialPropertyBlock propertyBlock;

    void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        ApplyColor();
    }

    // ================= VISUAL =================
    public void ApplyColor()
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        Color color = neutralColor;

        switch (team)
        {
            case Team.TeamA:
                color = teamAColor;
                break;

            case Team.TeamB:
                color = teamBColor;
                break;

            case Team.Neutral:
                color = neutralColor;
                break;
        }

        // Prefab overrides can keep a stale serialized renderer list. Refresh
        // it so Jeep, Tank and Truck always tint every visible mesh.
        renderers = GetComponentsInChildren<Renderer>(true);

        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            // Use a property block so each spawned vehicle receives its team
            // tint without replacing its material or erasing its Base Map.
            // Some imported URP materials expose _BaseColor while older ones
            // still use _Color, so write both when available.
            propertyBlock.Clear();
            Material material = rend.sharedMaterial;
            if (material != null && material.HasProperty("_BaseColor"))
                propertyBlock.SetColor("_BaseColor", color);
            if (material != null && material.HasProperty("_Color"))
                propertyBlock.SetColor("_Color", color);
            rend.SetPropertyBlock(propertyBlock);
        }
    }

    // ================= RUNTIME =================
    public void SetTeam(Team newTeam)
    {
        team = newTeam;
        ApplyColor();
    }
}
