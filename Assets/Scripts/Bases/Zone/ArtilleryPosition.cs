using UnityEngine;

public class ArtilleryPosition : MonoBehaviour
{
    public Transform spawnPoint;

    public bool occupied;

    public GameObject currentVehicle;

    public bool IsFree()
    {
        return !occupied;
    }

    public void Occupy(GameObject vehicle)
    {
        occupied = true;
        currentVehicle = vehicle;
    }

    public void Release()
    {
        occupied = false;
        currentVehicle = null;
    }
}