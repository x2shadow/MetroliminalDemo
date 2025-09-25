using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSignalSetPosition : MonoBehaviour
{
    [SerializeField] Transform position;
    [SerializeField] Transform position2;

    public void SetPosition()
    {
        if (position == null)
        {
            position = GameObject.Find("EndPosition").transform;
            transform.SetPositionAndRotation(position.position, position.rotation);
        }
        else transform.SetPositionAndRotation(position.position, position.rotation);

    }
    public void SetPosition2()
    {
        if (position2 == null)
        {
            position = GameObject.Find("EndPosition2").transform;
            transform.SetPositionAndRotation(position2.position, position2.rotation);
        }
        else transform.SetPositionAndRotation(position2.position, position2.rotation);
    }
}
