using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunMovement : MonoBehaviour
{
    public Vector2 speed;
    public Vector3 oldPosition;

    void Awake()
    {
        oldPosition = this.transform.position;
    }
    void Update()
    {
        this.transform.position = new Vector3(oldPosition.x, oldPosition.y - speed.x, oldPosition.z + speed.y);
    }
}
