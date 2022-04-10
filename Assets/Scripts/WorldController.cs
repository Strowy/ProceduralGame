using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    // Player
    public Transform player;
    // Terrain Controller
    public Transform terrainController;
    // Dungeon Controller
    public Transform dungeonController;
    // Replacement object once portal is used
    public Transform deadPortal;

    public int seedValue;

    // Player world coordinate marker
    private Vector3 playerLoc;
    // Event flag controller, location and instance;
    private int eventFlag;
    private Vector3 eventLoc;
    private string eventInstance;
    
    // Value which determines state of game for UI, etc.
    private int gameState;

    // List of cleared dungeons
    List<Tuple> clearedLocations;

    // Score
    private int gameScore;
    
    // Start is called before the first frame update
    void Start()
    {
        playerLoc = new Vector3();
        eventFlag = 0;
        clearedLocations = new List<Tuple>();
        gameScore = 0;
        gameState = -1;
    }

    // Update is called once per frame
    void Update()
    {
        // Check game has started
        if (gameState == -1) {}
        else if (gameState == 0)
        {
            // Activate player
            player.gameObject.SetActive(true);
            // Ensure activation of the environment controllers
            terrainController.gameObject.SetActive(true);
            dungeonController.gameObject.SetActive(true);
            gameState = 1;
        }
        else
        {
            // Check event flags
            if (eventFlag > 0)
            {
                switch (eventFlag)
                {
                    case 1:
                        // Dungeon entrance triggered in overworld
                        gameState = 2;

                        // Portal breaks once used
                        Transform portal = GameObject.Find(eventInstance.ToString()).transform;
                        Object.Instantiate(deadPortal, eventLoc, Quaternion.identity, portal.parent);
                        Destroy(portal.gameObject);


                        // Hide terrain (so it doesn't have to re-generate later as there's no change)
                        terrainController.gameObject.SetActive(false);

                        // Trigger dungeon generation
                        dungeonController.GetComponent<DungeonController>().SetSeedLocation(eventLoc);
                        dungeonController.GetComponent<DungeonController>().SetGoalFlag(true);

                        eventFlag = 0;
                        break;
                    case 2:
                        // Dungeon exit portal on deepest floor triggered in dungeon
                        gameState = 1;

                        player.GetComponent<CharacterController>().enabled = false;
                        terrainController.gameObject.SetActive(true);
                        player.transform.SetPositionAndRotation(playerLoc, player.transform.rotation);
                        player.GetComponent<CharacterController>().enabled = true;

                        // Mark dungeon as cleared and increase score
                        clearedLocations.Add(new Tuple((int)eventLoc.x, (int)eventLoc.z));
                        gameScore += 1;

                        eventFlag = 0;
                        break;
                    default:
                        break;
                }
            }
        }
    }

    // Check if dungeon entrance at given location is in the cleared list or not
    public bool CheckCleared(int x, int z)
    {
        bool chk = false;
        foreach (Tuple t in clearedLocations)
        {
            if (t.x == x && t.z == z) { chk = true; }
        }

        return chk;
    }

    public int GetScore() { return gameScore; }

    public int GetState() { return gameState; }
    
    public void MarkLocation( Vector3 loc ) { playerLoc = loc; }

    public void SetEventInstance( string evt ) { eventInstance = evt; }

    public void SetEventFlag( int flag ) { eventFlag = flag; }

    public void SetEventLocation( Vector3 loc ) { eventLoc = loc; }

    public void SetState( int st ) { gameState = st; }

    struct Tuple
    {
        public int x;
        public int z;

        public Tuple(int dx, int dz) { x = dx; z = dz; }
    }
}
