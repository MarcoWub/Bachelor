using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class Pool
{
    public GameObject[] items;
    public GameObject itemToPool;
    private int positionOfItemToUse = 0;

    public Pool(int size, GameObject item, Transform parent)
    {
        itemToPool = item;
        items = new GameObject[size];
        for (int i = 0; i < size; i++)
        {
            items[i] = UnityEngine.Object.Instantiate(itemToPool);
            items[i].transform.parent = parent;
            items[i].SetActive(false);
        }
            
    }

    public GameObject RequestItem()
    {
        if (positionOfItemToUse >= items.Length)
        {
            return default;           
        }


        var returnValue = items[positionOfItemToUse];
        positionOfItemToUse++;
        returnValue.SetActive(true);
        return returnValue;
    }

    public void ReturnItem(GameObject candidate)
    {
        positionOfItemToUse--;
        items[positionOfItemToUse] = candidate;
        candidate.SetActive(false);
    }
}
