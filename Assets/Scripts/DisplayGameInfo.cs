using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayGameInfo : MonoBehaviour
{
    public Text scoreText;
    public Text seedText;
    public Text floorText;
    
    private WorldController wc;
    
    // Start is called before the first frame update
    void Start()
    {
        wc = GameObject.FindGameObjectWithTag("WorldController").GetComponent<WorldController>();
        scoreText.text = "";
        floorText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        if (wc.GetState() > 0) { scoreText.text = "Cleared: " + wc.GetScore().ToString(); }
        if (wc.GetState() == 2)
        {
            DungeonController dc = GameObject.FindGameObjectWithTag("DungeonController").GetComponent<DungeonController>();
            floorText.text = "Floor: " + dc.GetCurrentFloor().ToString() + " / " + dc.nfloors;
        }
        else { floorText.text = ""; }
        
    }

    public void StartButtonClick()
    {
        // Check if seed input is parsible to int
        if (int.TryParse(seedText.text, out int result))
        {
            // If so, grab seed value and start game
            wc.seedValue = result;
            wc.SetState(0);

            // Turn off menu elements
            GameObject[] obj = GameObject.FindGameObjectsWithTag("Menu");
            foreach (GameObject gObj in obj) { gObj.SetActive(false); }
        }
    }
}
