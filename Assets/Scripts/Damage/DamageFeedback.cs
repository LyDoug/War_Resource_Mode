using System.Collections.Generic;
using UnityEngine;

// Runtime-only floating damage labels used while combat values are balanced.
public class DamageFeedback : MonoBehaviour
{
    struct Entry
    {
        public Vector3 worldPosition;
        public float damage;
        public bool armored;
        public float expiresAt;
    }

    static DamageFeedback instance;
    readonly List<Entry> entries = new List<Entry>();
    GUIStyle style;

    public static void Show(Vector3 worldPosition, float damage, bool armored)
    {
        if (damage <= 0f)
            return;

        if (instance == null)
        {
            GameObject host = new GameObject("Damage Feedback");
            instance = host.AddComponent<DamageFeedback>();
        }

        instance.entries.Add(new Entry
        {
            worldPosition = worldPosition + Vector3.up * 2.2f,
            damage = damage,
            armored = armored,
            expiresAt = Time.time + 0.9f
        });
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Update()
    {
        for (int index = entries.Count - 1; index >= 0; index--)
            if (Time.time >= entries[index].expiresAt)
                entries.RemoveAt(index);
    }

    void OnGUI()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        if (style == null)
            style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 18
            };

        Color old = GUI.color;
        foreach (Entry entry in entries)
        {
            Vector3 position = entry.worldPosition + Vector3.up * ((entry.expiresAt - Time.time) * 0.8f);
            Vector3 screen = camera.WorldToScreenPoint(position);
            if (screen.z <= 0f)
                continue;

            GUI.color = entry.armored ? new Color(0.25f, 0.85f, 1f) : new Color(1f, 0.8f, 0.2f);
            GUI.Label(new Rect(screen.x - 44f, Screen.height - screen.y - 14f, 88f, 28f),
                "-" + Mathf.CeilToInt(entry.damage), style);
        }
        GUI.color = old;
    }
}
