using UnityEngine;

public class ArtilleryCamera : MonoBehaviour
{
    public Camera artilleryCamera;

    public Camera playerCamera;

    void Start()
    {
        artilleryCamera.enabled = false;
    }

    public void EnableComputer()
    {
        playerCamera.enabled = false;
        artilleryCamera.enabled = true;
    }

    public void DisableComputer()
    {
        artilleryCamera.enabled = false;
        playerCamera.enabled = true;
    }
}