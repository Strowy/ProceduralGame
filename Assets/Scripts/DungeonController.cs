using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonController : MonoBehaviour
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
    private DungeonGenerator dungeonMap;
    // Chunk transform (all terrain objects stored within)
    private Transform dungeonChunk;
    
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
                GameObject.FindGameObjectWithTag("WorldController").GetComponent<WorldController>().SetEventFlag(2);
                // Reset floor count to zero
                currentFloor = 0;
            }

            goalReached = false;
        }
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
        dungeonMap.Generate(seedX, seedY);

        // Generate the dungeon cells
        for (int i = 0; i < dungeonX; i++)
        {
            for (int j = 0; j < dungeonY; j++)
            {
                // Calculate x,z (z = y in 2D space) offset and position
                float xPos = (cellSize * (i - 0.5f * dungeonX) + cellSize * 0.5f) + this.transform.position.x;
                float zPos = (cellSize * (j - 0.5f * dungeonY) + cellSize * 0.5f) + this.transform.position.z;
                float yPos;

                select = dungeonMap.GetMapVal(i, j);

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

public class DungeonGenerator
{
    // Map values
    private Tuple dims;
    private int[] map;
    // Max room size : max 2 * rSize + 1
    private readonly int rSize;
    // Max tunnel size : range tSize[0] to tSize[0] + tSize[2]
    private readonly int[] tSize;
    // Pseudo-random generator
    private PseudoRandomGenerator rng;
    // Complexity - essentially the number of attempts made to dig new tunnels/rooms
    private readonly int complexity;

    // Constructor
    public DungeonGenerator(int width, int height, int comp, int roomSize, int minTunnel, int maxTunnel)
    {
        dims.x = width;
        dims.y = height;
        map = new int[dims.x * dims.y];
        complexity = comp;
        rSize = roomSize;
        tSize = new int[2] { minTunnel, maxTunnel };
    }

    // Main generation function: overwrites this.map with a new map
    //  Generates pseudo-randomly from an (x,y) input
    public void Generate(int x, int y)
    {
        Tuple point, dir, size, bounds;
        int len;
        
        // Generation process counters
        int tickCounter, failCounter, failsPerTick;

        // Clear the map
        for (int i = 0; i < (dims.x * dims.y); i++) { map[i] = 0; }
        // Get prng (seed based on dungeon location in world). Bounded seed in range 0:2^14-1
        rng = new PseudoRandomGenerator(Mathf.Abs(x * (y + 13)) % 16384);

        // Generate seed room
        point = dims / 2;
        bounds = point / 2;

        size.x = rng.RandInt(rSize) + 1;
        size.y = rng.RandInt(rSize) + 1;

        point = SelectBoundedRandomPoint(point, bounds);
        CreateRoom(point, size);

        tickCounter = 0;
        failCounter = 0;
        failsPerTick = 10;
        
        // Generator loop
        do
        {
            // Find random room space
            do { point = SelectRandomPoint(); } while (map[point.x + point.y * dims.x] != 1);
            // Test for valid adjacent wall
            dir = TestForValidWall(point);
            // On fail increment fail count, else attempt tunnel generation
            if (dir.x == 0 && dir.y == 0) { failCounter++; }
            else
            {
                len = rng.RandInt(tSize[1] + 1) + tSize[0];
                // On creation fail increment fail count, else attempt room generation
                if (!TestForValidTunnel(point + dir, dir, len)) { failCounter++; }
                else
                {
                    size.x = rng.RandInt(rSize) + 1;
                    size.y = rng.RandInt(rSize) + 1;
                    // On creation fail increment fail count, else increment success and finish creation
                    if (!TestForValidRoom(point + dir * len, size)) { failCounter++; }
                    else
                    {
                        // Create tunnel and room
                        CreateTunnel(point + dir, dir, len);
                        CreateRoom(point + dir * len, size);
                        tickCounter++;
                    }
                }
            }

            // Check for overflow of failCounter
            if (failCounter >= failsPerTick)
            {
                failCounter -= failsPerTick;
                tickCounter++;
            }
        } while (tickCounter < complexity);

        // Find and create access points
        int select = rng.RandInt(4);
        int[,] quad = new int[4, 2] { { bounds.x, bounds.y }, { bounds.x * 3, bounds.y }, { bounds.x, bounds.y * 3 }, { bounds.x * 3, bounds.y * 3 } };
        Tuple pos;
        for (int i = 0; i < 2; i++)
        {
            // Switch to try to keep entrance / exit as far from each other as possible
            select = 3 - select;
            pos.x = quad[select, 0];
            pos.y = quad[select, 1];

            int n = 0;
            do
            { 
                point = SelectBoundedRandomPoint(pos, bounds - 1);
                n++;
            } while ((map[point.x + point.y * dims.x] != 1 || !(TestForValidAccess(point))) && n < 1000);

            if (n >= 1000)
            {
                do
                {
                    point = SelectBoundedRandomPoint(pos, dims - 2);
                } while (map[point.x + point.y * dims.x] != 1 || !(TestForValidAccess(point)));
            }
            CreateAccessPoint(point, i);
        }

        // Cleanup map and wallbound walkable areas
        for (int i = 0; i < dims.x * dims.y; i++)
        {
            if (map[i] == 2) { map[i] = 1; }
        }
        for (int i = 0; i < dims.x; i++)
        {
            for (int j = 0; j < dims.y; j++)
            {
                if (map[i + j * dims.x] == 0)
                {
                    Tuple p; p.x = i; p.y = j;
                    if (CheckConstruct(p) == true) { map[i + j * dims.x] = 2; }
                }
            }
        }
    }

    public int GetMapVal(int x, int y)
    {
        return map[x + y * dims.x];
    }

    // Construct a wall if any of near neighbours is a room/tunnel space
    private bool CheckConstruct(Tuple pos)
    {
        int k;
        // Map of checked spaces
        int[,] dir = new int[8, 2] { { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 } };

        k = 0;
        // Go through near neighbours, counting all spaces that are walkable
        for (int i = 0; i < dir.GetLength(0); i++)
        {
            int tx = pos.x + dir[i, 0];
            int ty = pos.y + dir[i, 1];
            // If out of bounds, do not count
            if (!(tx < 0 || tx >= dims.x || ty < 0 || ty >= dims.y))
            {
                if (map[tx + ty * dims.x] == 1) { k++; }
            }
        }

        // Wall constructable if any room spaces
        if (k == 0) { return false; }
        else { return true; }
    }

    // Check surrounding 8 spaces of given position
    private bool CheckNearNeighbours(Tuple pos)
    {
        int k;
        // Map of checked spaces
        int[,] dir = new int[8,2] { { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 } };

        k = 0;
        // Go through near neighbours, counting all spaces that are not rooms
        for (int i = 0; i < dir.GetLength(0); i++)
        {
            int tx = pos.x + dir[i, 0];
            int ty = pos.y + dir[i, 1];
            // If out of bounds, failure and considered a room
            if (!(tx < 0 || tx >= dims.x || ty < 0 || ty >= dims.y))
            {
                if (map[tx + ty * dims.x] != 1) { k++; }
            }
        }

        // Position invalid if any room spaces
        if (k == 8) { return true; }
        else { return false; }
    }

    // Creates access point : Entrance (3) if chk is 0, else Exit (4)
    private void CreateAccessPoint(Tuple pos, int chk)
    {
        if (chk == 0) { map[pos.x + pos.y * dims.x] = 3; }
        else { map[pos.x + pos.y * dims.x] = 4; }
    }
    
    // Creates a room centred on position with given size
    //  size = ([size of room] - 1) / 2: all rooms are of odd size in both dimensions
    private void CreateRoom(Tuple pos, Tuple size)
    {
        for (int i = pos.x - size.x; i <= pos.x + size.x; i++)
        {
            for (int j = pos.y - size.y; j <= pos.y + size.y; j++)
            {
                map[i + j * dims.x] = 1;
            }
        }
    }

    // Creates a tunnel (1 width, len length) at given position in given direction
    private void CreateTunnel(Tuple pos, Tuple dir, int len)
    {
        for (int i = 0; i < len; i++)
        {
            map[(pos.x + dir.x * i) + (pos.y + dir.y * i) * dims.x] = 2;
        }
    }

    // Select random point on map within bounded range of a given point with edge buffer
    private Tuple SelectBoundedRandomPoint(Tuple pos, Tuple bounds)
    {
        Tuple point;

        // Buffer the point, but prevent inf. loop by poor bounding or position
        int n = 0;
        do
        {
            point.x = rng.RandInt(2 * bounds.x + 1) + (pos.x - bounds.x);
            point.y = rng.RandInt(2 * bounds.y + 1) + (pos.y - bounds.y);
            n++;
        } while ((point.x < 2 || point.x > dims.x - 3 || point.y < 2 || point.y > dims.y - 3) && n < 1000);
        
        // Assign centre of map as default if broken due to loop count
        if (n >= 1000) { point = dims / 2; }

        return point;
    }

    // Select random point on the map with 2 point edge buffer
    private Tuple SelectRandomPoint()
    {
        Tuple point;
        point.x = rng.RandInt(dims.x - 4) + 2;
        point.y = rng.RandInt(dims.y - 4) + 2;

        return point;
    }

    // Test for valid access point (entrance / exit)
    private bool TestForValidAccess(Tuple pos)
    {
        // Only the four cardinal directions are necessary as there are no concave rooms
        int[,] dir = new int[4, 2] { { -1, 0 }, { 0, -1 }, { 1, 0 }, { 0, 1 } };

        bool t = false;
        int i = 0;
        // floor test
        while (i < 4 && !t)
        {
            int tx = pos.x + dir[i, 0];
            int ty = pos.y + dir[i, 1];

            // If any space is not floor, it's a failure
            if (map[tx + ty * dims.x] != 1) { t = true; }
            
            i++;
        }

        // If i = 4 AND t is false all spaces were floors
        if (i == 4 && !t) { return true; }
        else { return false; }
    }
    
    // Test for valid room centered on position with given size
    private bool TestForValidRoom(Tuple pos, Tuple size)
    {
        int k;
        // Map of the checked spaces
        int[,] dir = new int[9,2] { { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 }, { 0, 0 } };

        k = 0;
        // Check the 9 points (corners, midpoints, centre)
        for (int i = 0; i < dir.GetLength(0); i++)
        {
            Tuple t;
            t.x = pos.x + dir[i, 0] * size.x;
            t.y = pos.y + dir[i, 1] * size.y;
            if (!CheckNearNeighbours(t)) { k++; }
        }

        // If any failures, would overwrite another room, so a failure.
        if (k > 0) { return false; }
        else { return true; }
    }

    // Test for valid tunnel starting at position in direction with given length
    private bool TestForValidTunnel(Tuple pos, Tuple dir, int len)
    {
        bool t = true;
        // Check the planned space is entirely filled in
        for (int i = 0; i < len; i++)
        {
            int tx = pos.x + dir.x * i;
            int ty = pos.y + dir.y * i;

            // Check buffer
            if (!(tx < 2 || tx > dims.x - 3 || ty < 2 || ty > dims.y - 3))
            {
                if (map[tx + ty * dims.x] != 0) { t = false; }
            }
            else { t = false; }
        }

        return t;
    }

    private Tuple TestForValidWall(Tuple pos)
    {
        Tuple val; val.x = 0; val.y = 0;

        // The 4 spaces left, up, right, down are the relevant spaces to test
        //  however if not searched in randomized order, causes a directional bias in room generation
        int[,] dir = new int[4, 2] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };

        // Get (x,y) being -1 or 1, and another int to determine order, then generate test array
        int rx = rng.RandInt(2); rx = (rx == 0) ? -1 : 1;
        int ry = rng.RandInt(2); ry = (ry == 0) ? -1 : 1;
        int ro = rng.RandInt(2);

        if (ro == 0) { dir[0, 1] += ry; dir[1, 0] += rx; dir[2, 1] -= ry; dir[3, 0] -= rx; }
        else { dir[0, 0] += rx; dir[1, 1] += ry; dir[2, 0] -= rx; dir[3, 1] -= ry; }

        bool t = false;
        int i = 0;
        // Wall test
        while (i < 4 && !t)
        {
            int tx = pos.x + dir[i, 0];
            int ty = pos.y + dir[i, 1];
            // if wall, check adjacent spaces are also walls - if so, space is valid wall
            if (map[tx + ty * dims.x] == 0)
            {
                if(map[(tx + dir[i,1]) + (ty + dir[i,0]) * dims.x] == 0 && map[(tx - dir[i, 1]) + (ty - dir[i, 0]) * dims.x] == 0)
                {
                    val.x = dir[i, 0];
                    val.y = dir[i, 1];
                    t = true;
                }
            }

            i++;
        }
        
        // If i = 4 no valid walls were found
        if (i == 4) { val.x = 0; val.y = 0; }

        return val;
    }

    // Tuple
    struct Tuple
    {
        public int x;
        public int y;

        public static Tuple operator +(Tuple a, Tuple b)
        {
            Tuple c;
            c.x = a.x + b.x;
            c.y = a.y + b.y;

            return c;
        }
        public static Tuple operator -(Tuple a, int b)
        {
            Tuple c;
            c.x = a.x - b;
            c.y = a.y - b;

            return c;
        }

        public static Tuple operator *(Tuple a, int b)
        {
            Tuple c;
            c.x = a.x * b;
            c.y = a.y * b;

            return c;
        }

        public static Tuple operator /(Tuple a, int b)
        {
            Tuple c;
            c.x = a.x / b;
            c.y = a.y / b;

            return c;
        }
    }
}