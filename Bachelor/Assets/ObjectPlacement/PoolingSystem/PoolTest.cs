using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolTest : MonoBehaviour
{
    public GameObject clone;
    public Pool pool;
    // Start is called before the first frame update
    void Start()
    {
        pool = new Pool(10, clone, this.transform);
    }


}
