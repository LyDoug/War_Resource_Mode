using UnityEngine;

public class ArtilleryPositionManager : MonoBehaviour
{
    public ArtilleryPosition[] positions;

    public ArtilleryPosition GetFreePosition()
    {
        foreach (ArtilleryPosition pos in positions)
        {
            if (pos.IsFree())
                return pos;
        }

        return null;
    }
}