using UnityEngine;

public class FlagController : MonoBehaviour
{
    [Header("Team")]
    public TeamMember teamMember;

    [Header("Animation")]
    public Transform flagVisual; // objeto que sobe
    public float raisedHeight = 3f;
    public float loweredHeight = 0f;
    public float moveSpeed = 3f;
    public float forwardOffset = 2f;

    float targetHeight;
    bool runtimeFlag;

    void Awake()
    {
        if (flagVisual == null)
        {
            flagVisual = CreateRuntimeFlag();
            runtimeFlag = true;
        }

        if (teamMember == null)
            teamMember = flagVisual.GetComponent<TeamMember>();

        if (teamMember == null)
            teamMember = flagVisual.gameObject.AddComponent<TeamMember>();

        targetHeight = loweredHeight;
    }

    Transform CreateRuntimeFlag()
    {
        GameObject flag = new GameObject("Team Flag Marker");
        // Capture zones often have a very large scale.  A child cube would
        // inherit it and turn into a giant flag, so runtime flags live in
        // world space and follow the capture zone in Update.
        flag.transform.position = transform.position + transform.forward * forwardOffset;
        flag.transform.rotation = Quaternion.identity;

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Flag Pole";
        pole.transform.SetParent(flag.transform, false);
        pole.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        pole.transform.localScale = new Vector3(0.09f, 1.1f, 0.09f);
        Destroy(pole.GetComponent<Collider>());
        pole.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.2f);

        GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        banner.name = "Team Banner";
        banner.transform.SetParent(flag.transform, false);
        banner.transform.localPosition = new Vector3(0.62f, 1.75f, 0f);
        banner.transform.localScale = new Vector3(1.2f, 0.62f, 0.05f);
        Destroy(banner.GetComponent<Collider>());

        TeamMember colorMember = flag.AddComponent<TeamMember>();
        colorMember.renderers = new[] { banner.GetComponent<Renderer>() };

        return flag.transform;
    }

    void Update()
    {
        if (runtimeFlag)
        {
            Vector3 pos = flagVisual.position;
            Vector3 anchor = transform.position + transform.forward * forwardOffset;
            pos.x = anchor.x;
            pos.z = anchor.z;
            pos.y = Mathf.Lerp(pos.y, anchor.y + targetHeight,
                Time.deltaTime * moveSpeed);
            flagVisual.position = pos;
            return;
        }

        Vector3 localPos = flagVisual.localPosition;
        localPos.y = Mathf.Lerp(localPos.y, targetHeight, Time.deltaTime * moveSpeed);
        flagVisual.localPosition = localPos;
    }

    // ================= CAPTURA =================
    public void SetOwner(Team team)
    {
        if (teamMember != null)
        {
            teamMember.SetTeam(team); // 🔥 MUDA COR
        }

        // Neutral structures keep the flag low; an owner raises it above the
        // building so a factory/mine/city reads as useful from a distance.
        targetHeight = team == Team.Neutral ? loweredHeight : raisedHeight;

        Debug.Log("🚩 Flag atualizada para: " + team);
    }

    // opcional: reset
    public void ResetFlag()
    {
        if (teamMember != null)
            teamMember.SetTeam(Team.Neutral);

        targetHeight = loweredHeight;
    }
}
