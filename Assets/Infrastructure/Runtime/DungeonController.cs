using AIR.Flume;
using Application.Interfaces;
using Domain;
using Infrastructure.Runtime.Dungeon.Generators;
using UnityEngine;

public class DungeonController : DependentBehaviour
{
    // Transform for player character to determine position
    public Transform player;

    // Dungeon variables
    public int dungeonX;        // Dungeon width
    public int dungeonY;        // Dungeon height
    public int nfloors;         // Number of floors in dungeon
    public int complexity;      // Complexity of dungeon
    public int roomSize;        // Max. size of rooms : Size = 2 * roomSize + 1
    public int minTunnel;       // Min. tunnel length
    public int varTunnel;       // Variability of tunnel length
    public float cellSize;      // Physical width of map cells : avoid changing if possible

    // Map piece that is instantiated
    public Transform dungeonObject;
    // Materials for the various terrain types
    public Material[] dungeonMaterials;
    // Portal Object
    public Transform dungeonPortal;

    // Dungeon goal flags, counts
    private bool goalReached;
    private int currentFloor;
    
    // Seed location values
    private int baseX, baseY, seedX, seedY;

    // Generator
    private IDungeonGenerator dungeonMap;
    // Chunk transform (all terrain objects stored within)
    private Transform dungeonChunk;

    private IGameStateController _gameStateController;

    public void Inject(IGameStateController gameStateController)
    {
        _gameStateController = gameStateController;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        dungeonMap = new DungeonGenerator(dungeonX, dungeonY, complexity, roomSize, minTunnel, varTunnel);

        // Initial settings
        currentFloor = 0;
        goalReached = false;
        dungeonChunk = new GameObject("Dungeon Chunk").transform;
        dungeonChunk.position = this.transform.position;
        dungeonChunk.parent = this.transform;

        // Correct size of terrain cells
        dungeonObject.localScale = new Vector3(cellSize, cellSize * 4, cellSize);
    }

    // Update is called once per frame
    void Update()
    {
        // If a portal (at exit) is entered
        if (goalReached)
        {
            // Erase the current floor if there is one
            if (dungeonChunk)
            {
                Object.Destroy(dungeonChunk.gameObject);
            }

            if (currentFloor < nfloors)
            {
                // Generate new floor
                GenerateNewFloor();
                currentFloor++;
            }
            else
            {
                // Set flag that goal achieved (reached portal on lowest floor)
                ExitDungeon();
                // Reset floor count to zero
                currentFloor = 0;
            }

            goalReached = false;
        }
    }

    private void ExitDungeon()
    {
        _gameStateController.TriggerEventInstance("", PortalType.Exit, Vector3.zero);
    }

    // Create new dungeon floor
    void GenerateNewFloor()
    {
        int select;
        Transform mCell;

        dungeonChunk = new GameObject("Dungeon Chunk").transform;
        dungeonChunk.position = this.transform.position;
        dungeonChunk.parent = this.transform;

        // Create floor map in generator
        dungeonMap.Generate(new IntegerPoint(seedX, seedY));

        // Generate the dungeon cells
        for (int i = 0; i <= dungeonX; i++)
        {
            for (int j = 0; j <= dungeonY; j++)
            {
                // Calculate x,z (z = y in 2D space) offset and position
                float xPos = (cellSize * (i - 0.5f * dungeonX) + cellSize * 0.5f) + this.transform.position.x;
                float zPos = (cellSize * (j - 0.5f * dungeonY) + cellSize * 0.5f) + this.transform.position.z;
                float yPos;

                select = dungeonMap.Map.Cells[i + j * dungeonMap.Map.Bounds.Width];

                // Instantiate cells with type values greater than 0
                if (select > 0)
                {
                    switch (select)
                    {
                        case 1:
                            // Walkable ground
                            yPos = cellSize + this.transform.position.y;
                            mCell = Object.Instantiate(dungeonObject, new Vector3(xPos, yPos, zPos), Quaternion.identity, dungeonChunk);
                            mCell.GetComponent<MeshRenderer>().material = dungeonMaterials[0];
                            mCell.localScale = new Vector3(mCell.localScale.x, mCell.localScale.y / 2, mCell.localScale.z);
                            break;
                        case 2:
                            // Walls
                            yPos = 2 * cellSize + this.transform.position.y;
                            mCell = Object.Instantiate(dungeonObject, new Vector3(xPos, yPos, zPos), Quaternion.identity, dungeonChunk);
                            mCell.GetComponent<MeshRenderer>().material = dungeonMaterials[1];
                            BoxCollider boxCollider = mCell.GetComponent<BoxCollider>();
                            boxCollider.center = new Vector3(0, 3.5f, 0);
                            boxCollider.size = new Vector3(1, 8, 1);
                            break;
                        case 3:
                            // Entrance
                            yPos = cellSize + this.transform.position.y;
                            mCell = Object.Instantiate(dungeonObject, new Vector3(xPos, yPos, zPos), Quaternion.identity, dungeonChunk);
                            mCell.GetComponent<MeshRenderer>().material = dungeonMaterials[0];
                            mCell.localScale = new Vector3(mCell.localScale.x, mCell.localScale.y / 2, mCell.localScale.z);

                            // Set player position
                            player.GetComponent<CharacterController>().enabled = false;
                            player.transform.SetPositionAndRotation(new Vector3(xPos, yPos + cellSize + 1.1f, zPos), player.transform.rotation);
                            player.GetComponent<CharacterController>().enabled = true;

                            break;
                        case 4:
                            // Exit
                            yPos = cellSize + this.transform.position.y;
                            mCell = Object.Instantiate(dungeonObject, new Vector3(xPos, yPos, zPos), Quaternion.identity, dungeonChunk);
                            mCell.GetComponent<MeshRenderer>().material = dungeonMaterials[0];
                            mCell.localScale = new Vector3(mCell.localScale.x, mCell.localScale.y / 2, mCell.localScale.z);

                            // Create portal
                            Object.Instantiate(dungeonPortal, new Vector3(xPos, yPos + cellSize, zPos), Quaternion.identity, dungeonChunk);
                            break;
                        default:
                            // NULL
                            break;
                    }
                }
            }
        }
    }

    public int GetCurrentFloor() { return currentFloor; }

    public void SetSeedLocation( Vector3 loc )
    {
        baseX = (int)loc.x;
        baseY = (int)loc.z;

        seedX = baseX;
        seedY = baseY;
    }

    public void SetGoalFlag( bool flg ) { goalReached = flg; }

    public void UpdateSeedLocation( Vector3 loc )
    {
        seedX = baseX + (int)loc.x + currentFloor;
        seedY = baseY + (int)loc.z + currentFloor;
    }
}