using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSignalSetPosition : MonoBehaviour
{
    [SerializeField]
    Transform position;

    public void SetPosition()
    {
        if (position == null)
        {
            position = GameObject.Find("EndPosition").transform;
            transform.SetPositionAndRotation(position.position, position.rotation);
        }
        else transform.SetPositionAndRotation(position.position, position.rotation);
    }
}
