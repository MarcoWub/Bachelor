using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveWithPlayer : MonoBehaviour
{

    public Transform player;
    void Update()
    {
        this.transform.position = new Vector3(player.position.x, this.transform.position.y, player.position.z);
    }
}
