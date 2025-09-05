using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSignalSetPosition : MonoBehaviour
{
    [SerializeField]
    Transform position;

    public void SetPosition()
    {
        transform.SetPositionAndRotation(position.position, position.rotation);
    }
}
