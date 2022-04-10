using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal_Dungeon : MonoBehaviour
{
    private DungeonController dc;
    
    // Start is called before the first frame update
    void Start()
    {
        dc = GameObject.FindGameObjectWithTag("DungeonController").GetComponent<DungeonController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            dc.SetGoalFlag(true);
            dc.UpdateSeedLocation(this.transform.position);
        }
    }
}
