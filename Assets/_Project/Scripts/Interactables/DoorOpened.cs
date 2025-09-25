using UnityEngine;

public class DoorOpened : MonoBehaviour, IInteractable
{
    public bool used = false;

    public void Interact(PlayerController player)
    {
        if (used) return;
        Debug.Log("Дверь открыта!");
        GetComponent<Animation>().Play();
        used = true;
    }

    public bool GetUsed()
    {
        return used;
    }
}
