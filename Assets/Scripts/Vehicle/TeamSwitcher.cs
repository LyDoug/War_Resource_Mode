using UnityEngine;

public class TeamSwitcher : MonoBehaviour
{
    private TeamMember player;

    void Start()
    {
        // tenta pegar automaticamente no próprio objeto
        player = GetComponent<TeamMember>();

        // se não tiver, procura na cena
        if (player == null)
        {
            player = FindObjectOfType<TeamMember>();
        }

        if (player == null)
        {
            Debug.LogError("❌ Nenhum TeamMember encontrado na cena!");
        }
    }

    public void SetTeam(Team newTeam)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectPlayerTeam(newTeam);
            return;
        }

        if (player == null) return;

        player.SetTeam(newTeam);

        Debug.Log("🔄 Time alterado para: " + newTeam);
    }

}
