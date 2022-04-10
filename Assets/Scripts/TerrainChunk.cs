using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
    // Map piece that is instantiated
    public Transform terrainObject;
    // Portals both active and dead
    public Transform portal_Active;
    public Transform portal_Dead;
    // Materials for the various terrain types
    public Material[] terrainMaterials;

    private int seed;
    private MapCellProcessor mapper;
    private float cellSize;
    private int chunkSize;
    private int biomeSize;
    private int maxHeight;

    // Start is called before the first frame update
    void Start()
    {
        // Grab required values from the controller
        TerrainController controller = this.GetComponentInParent<TerrainController>();
        seed = controller.seedValue;
        cellSize = controller.cellSize;
        chunkSize = controller.chunkSize;
        biomeSize = controller.biomeSize;
        maxHeight = controller.maxHeight;
        
        // Get the map processor
        mapper = new MapCellProcessor(seed, maxHeight, (int)(biomeSize * cellSize));

        // Correct size of terrain cells
        terrainObject.localScale = new Vector3(cellSize, cellSize * 4, cellSize);
        
        // Generate the terrain cells
        // terrain chunks are square
        for (int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkSize; j++)
            {
                // Calculate x,z (z = y in 2D space) offset and position
                float xPos = (cellSize * (i - 0.5f * chunkSize) + cellSize * 0.5f) + this.transform.position.x;
                float zPos = (cellSize * (j - 0.5f * chunkSize) + cellSize * 0.5f) + this.transform.position.z;

                // Get cell data based on position
                int[] dataCell = mapper.CellData((int)(xPos / cellSize), (int)(zPos / cellSize));
                float yPos = dataCell[0] + this.transform.position.y;

                Transform mCell = Object.Instantiate(terrainObject, new Vector3(xPos, yPos, zPos), Quaternion.identity, this.transform);
                mCell.GetComponent<MeshRenderer>().material = terrainMaterials[dataCell[1]];
                
                // Special terrains
                // ----- //
                // If water terrain
                if (dataCell[2] == 1)
                {
                    BoxCollider boxCollider = mCell.GetComponent<BoxCollider>();
                    boxCollider.center = new Vector3(0, 3.5f, 0);
                    boxCollider.size = new Vector3(1, 8, 1);
                }
                // If Dungeon Portal
                if (dataCell[2] == 2)
                {
                    mCell.GetComponent<MeshRenderer>().material = terrainMaterials[8];

                    // Check if the dungeon is on the list of cleared dungeons
                    if (GameObject.FindGameObjectWithTag("WorldController").GetComponent<WorldController>().CheckCleared((int)xPos, (int)zPos))
                    {
                        Object.Instantiate(portal_Dead, new Vector3(xPos, yPos + cellSize * 2, zPos), Quaternion.identity, this.transform);
                    }
                    else
                    {
                        Object.Instantiate(portal_Active, new Vector3(xPos, yPos + cellSize * 2, zPos), Quaternion.identity, this.transform);
                    }  
                }
                // If Dungeon Surrounds
                if (dataCell[2] == 3)
                {
                    mCell.GetComponent<MeshRenderer>().material = terrainMaterials[8];
                }
                if (dataCell[2] == 4)
                {
                    mCell.transform.position = new Vector3(xPos, yPos + 2 * cellSize, zPos);
                    mCell.GetComponent<MeshRenderer>().material = terrainMaterials[9];
                }

            }
        }
    }
}

public class MapCellProcessor
{
    private readonly int maxHeight;
    private readonly float[,] terrainModifiers;
    private readonly int biomeSize;

    private PerlinNoise mapNoise;
    private PseudoRandomGenerator rNumber;
    

    public MapCellProcessor(int seed, int mHeight, int bSize)
    {
        mapNoise = new PerlinNoise(seed);
        rNumber = new PseudoRandomGenerator(seed);
        maxHeight = mHeight;
        biomeSize = bSize;

        terrainModifiers = new float[8, 3];
        // Height boundaries of each terrain type
        terrainModifiers[0, 0] = 0;
        terrainModifiers[1, 0] = 0.28f;
        terrainModifiers[2, 0] = 0.30f;
        terrainModifiers[3, 0] = 0.35f;
        terrainModifiers[4, 0] = 0.45f;
        terrainModifiers[5, 0] = 0.65f;
        terrainModifiers[6, 0] = 0.75f;
        terrainModifiers[7, 0] = 0.90f;
        // Height multipliers of each terrain type
        terrainModifiers[0, 1] = 0;
        terrainModifiers[1, 1] = 0;
        terrainModifiers[2, 1] = 0.25f;
        terrainModifiers[3, 1] = 0.50f;
        terrainModifiers[4, 1] = 0.25f;
        terrainModifiers[5, 1] = 1.00f;
        terrainModifiers[6, 1] = 3.00f;
        terrainModifiers[7, 1] = 1.00f;
        // Sum (b - a) * n where a, b are the lower, upper bounds of each terrain type
        terrainModifiers[0, 2] = 0;
        for (int i = 1; i < 8; i++) { terrainModifiers[i, 2] = terrainModifiers[i - 1, 1] * (terrainModifiers[i, 0] - terrainModifiers[i - 1, 0]) + terrainModifiers[i - 1, 2]; }

    }

    // Main function to produce map cell information. Returns [height, terrain type, special data]
    public int[] CellData(int x, int y)
    {
        int[] g = new int[4] { 0, 0, 0, 0 };
        float hVal;
        int bx, by;
        Tuple spc;
        int n = 8; // 8 types of base terrain

        // Biome x, y values
        bx = Mathf.FloorToInt((float)x / biomeSize);
        by = Mathf.FloorToInt((float)y / biomeSize);

        // Get noise map value
        hVal = mapNoise.Perlin2D(x, y);

        // Get terrain type then calculate height and other values from that
        g[1] = TerrainType(hVal, n);
        // Special terrain type check
        spc = SpecialTerrainType(x, y, g[1], bx, by);
        g[2] = spc.a; g[3] = spc.b;
        // Terrain height
        g[0] = TerrainHeight(hVal, g);

        return g;
    }

    // Test for special types of terrain
    private Tuple SpecialTerrainType(int x, int y, int tType, int bx, int by)
    {
        // Type values:
        // 1: Water terrain
        // 2: Dungeon Portal
        // 3-10: Dungeon Surroundings


        Tuple r = new Tuple(0, 0);
        int dx, dy;
        int de;

        dx = x - (bx * biomeSize);
        dy = y - (by * biomeSize);

        // If water terrain
        if (tType == 0 || tType == 1) { r.a = 1; }
        
        // Check for Dungeon Portals (only appear on certain terrain)
        if (tType == 4)
        {
            // Make it so it cannot appear on border cells of biome
            de = rNumber.IntVal(bx, by, (biomeSize - 4) * (biomeSize - 4));
            de = (Mod(de, biomeSize - 4) + 2) + (Mathf.FloorToInt((float)de / (biomeSize - 4)) + 2) * biomeSize;

            if (dx + dy * biomeSize == de) { r.a = 2; }
        }
        // Check for Dungeon Surroundings
        if ((tType == 3 || tType == 4 || tType == 5) && (r.a != 2))
        {
            int ex, ey;

            // Make it so portal cannot appear on border cells of biome (otherwise walls would cut off)
            de = rNumber.IntVal(bx, by, (biomeSize - 4) * (biomeSize - 4));
            de = (Mod(de, biomeSize - 4) + 2) + (Mathf.FloorToInt((float)de / (biomeSize - 4)) + 2) * biomeSize;
            ex = Mod(de, biomeSize);
            ey = Mathf.FloorToInt((float)de / biomeSize);

            // Creates a wall in the eight spaces around the dungeon portal
            if ((Mathf.Abs(dx - ex) < 3) && (Mathf.Abs(dy - ey) < 3))
            {
                float hVal = mapNoise.Perlin2D(x - (dx - ex), y - (dy - ey));
                if (TerrainType(hVal, 8) == 4)
                {
                    r.a = 3;
                    r.b = TerrainHeight(hVal, new int[4] { 0, 4, 2, 0});

                    if (((Mathf.Abs(dx - ex) > 0) && (Mathf.Abs(dy - ey) > 1)) || ((Mathf.Abs(dx - ex) > 1) && (Mathf.Abs(dy - ey) > 0)))
                    {
                        r.a = 4;
                    }

                }
            }
        }


        return r;
    }

    // Determines terrain type from height map, which affects height and material type
    // Input height is float in range [0, 1], n is number of different terrain types
    private int TerrainType(float height, int n)
    {
        int r = 0;

       for (int i = 0; i < n; i++)
        {
            if (height > terrainModifiers[i,0]) { r = i; }
        }

        return r;
    }

    private int TerrainHeight(float height, int[] vals)
    {
        int r = 0;
        
        if (vals[2] < 3) // Unmodified height values
        {
            r = (int)(((height - terrainModifiers[vals[1], 0]) * terrainModifiers[vals[1], 1] + terrainModifiers[vals[1], 2]) * maxHeight);
        }
        else if (vals[2] == 3) // Dungeon surround height values
        {
            r = vals[3];
        }
        else if (vals[2] == 4)
        {
            r = vals[3];
        }
        
        return r;
    }

    private int Mod(int a, int b)
    {
        if (a < 0) { return (a % b + b) % b; }
        else { return a % b; }
    }

    struct Tuple
    {
        public int a;
        public int b;

        public Tuple(int da, int db) { a = da; b = db; }
    }
}