using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal_World : MonoBehaviour
{
    private WorldController wc;

    // Start is called before the first frame update
    void Start()
    {
        wc = GameObject.FindGameObjectWithTag("WorldController").GetComponent<WorldController>();
        this.transform.gameObject.name = this.transform.gameObject.GetInstanceID().ToString();
    }

    void OnTriggerEnter(Collider other)
    {
        // Player enters portal
        if (other.gameObject.CompareTag("Player"))
        {
            wc.MarkLocation(other.gameObject.transform.position);
            wc.SetEventLocation(this.transform.position);
            wc.SetEventFlag(1);
            wc.SetEventInstance(this.transform.gameObject.name);
        }
    }
}
