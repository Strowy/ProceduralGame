using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
    // Map piece that is instantiated
    public Transform terrainObject;

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
                }
                // If Dungeon Surrounds
                if (dataCell[2] >= 3 && dataCell[2] < 11)
                {
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
        int[] g = new int[3];
        float hVal;
        int bx, by;
        int n = 8; // 8 types of base terrain

        // Biome x, y values
        bx = Mathf.FloorToInt((float)x / biomeSize);
        by = Mathf.FloorToInt((float)y / biomeSize);

        // Get noise map value
        hVal = mapNoise.Perlin2D(x, y);

        // Get terrain type then calculate height and other values from that
        g[1] = TerrainType(hVal, n);
        // Special terrain type check
        g[2] = SpecialTerrainType(x, y, g[1], bx, by);
        // Terrain height
        g[0] = TerrainHeight(x, y, hVal, g[1], g[2]);

        return g;
    }

    // Test for special types of terrain
    private int SpecialTerrainType(int x, int y, int tType, int bx, int by)
    {
        // Type values:
        // 1: Water terrain
        // 2: Dungeon Portal
        // 3-10: Dungeon Surroundings
        
        int r = 0;
        int dx, dy;
        int de;

        dx = x - (bx * biomeSize);
        dy = y - (by * biomeSize);

        // If water terrain
        if (tType == 0 || tType == 1) { r = 1; }
        
        // Check for Dungeon Portals (only appear on certain terrain)
        if (tType == 4)
        {
            // Make it so it cannot appear on border cells of biome
            de = rNumber.IntVal(bx, by, (biomeSize - 2) * (biomeSize - 2));
            de = (Mod(de, biomeSize - 2) + 1) + (Mathf.FloorToInt((float)de / (biomeSize - 2)) + 1) * biomeSize;

            if (dx + dy * biomeSize == de) { r = 2; }
        }
        // Check for Dungeon Surroundings
        if ((tType == 3 || tType == 4 || tType == 5) && (r != 2))
        {
            int ex, ey;

            // Make it so portal cannot appear on border cells of biome (otherwise walls would cut off)
            de = rNumber.IntVal(bx, by, (biomeSize - 2) * (biomeSize - 2));
            de = (Mod(de, biomeSize - 2) + 1) + (Mathf.FloorToInt((float)de / (biomeSize - 2)) + 1) * biomeSize;
            ex = Mod(de, biomeSize);
            ey = Mathf.FloorToInt((float)de / biomeSize);

            // Creates a wall in the eight spaces around the dungeon portal
            if ((Mathf.Abs(dx - ex) < 2) && (Mathf.Abs(dy - ey) < 2))
            {
                if (TerrainType(mapNoise.Perlin2D(x - (dx - ex), y - (dy - ey)), 8) == 4)
                {
                    int[,] wallId = new int[8, 2] { { -1, -1 }, { 0, -1 }, { 1, -1 }, { -1, 0 }, { 1, 0 }, { -1, 1 }, { 0, 1 }, { 1, 1 } };

                    for (int i = 0; i < 8; i++)
                    {
                        if (dx - ex == wallId[i, 0] && dy - ey == wallId[i, 1]) { r = i + 3; }
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

    private int TerrainHeight(int x, int y, float height, int n, int spec)
    {
        int r = 0;
        
        if (spec < 3) // Unmodified height values
        {
            r = (int)(((height - terrainModifiers[n, 0]) * terrainModifiers[n, 1] + terrainModifiers[n, 2]) * maxHeight);
        }
        else if (spec >= 3 && spec < 11) // Dungeon surround height values
        {
            float hVal;
            int[] dir = new int[2];
            int[,] wallId = new int[8, 2] { { -1, -1 }, { 0, -1 }, { 1, -1 }, { -1, 0 }, { 1, 0 }, { -1, 1 }, { 0, 1 }, { 1, 1 } };
            int[] ent = new int[4] { 4, 6, 7, 9 };

            dir[0] = wallId[spec - 3, 0];
            dir[1] = wallId[spec - 3, 1];

            hVal = mapNoise.Perlin2D(x - dir[0], y - dir[1]);
            n = TerrainType(hVal, 8);

            r = (int)(((hVal - terrainModifiers[n, 0]) * terrainModifiers[n, 1] + terrainModifiers[n, 2]) * maxHeight);

            // Create entrance, walls
            if (spec != ent[rNumber.IntVal(x - dir[0], y - dir[1], 4)]) { r += 4; }
        }
        
        return r;
    }

    private int Mod(int a, int b)
    {
        if (a < 0) { return (a % b + b) % b; }
        else { return a % b; }
    }
}

public class PerlinNoise
{
    // Gradient permutation array and its period
    private readonly int[,] gradient = {
        {1,1,0}, {-1,1,0}, {1,-1,0}, {-1,-1,0},
        {1,0,1}, {-1,0,1}, {1,0,-1}, {-1,0,-1},
        {0,1,1}, {0,-1,1}, {0,1,-1}, {0,-1,-1},
        {1,1,0}, {0,-1,1}, {-1,1,0}, {0,-1,-1}};

    private readonly int period;

    // Settings for Perlin noise generation
    private PseudoRandomGenerator rng;
    private int gridSize;
    private int octaves;
    private float persistence;

    // Constructor
    public PerlinNoise(int seed)
    {
        period = gradient.GetLength(0);
        rng = new PseudoRandomGenerator(seed);
        gridSize = 64;
        octaves = 3;
        persistence = 0.5f;
    }

    // Returns a bounded float in [0, 1]
    public float Perlin2D(int x, int y)
    {
        // Values for multi-octave calculation
        float valueSum, amp, maxVal;
        int freq;
        
        valueSum = 0f;
        freq = 1;
        amp = 1.0f;
        maxVal = 0f;

        for (int i = 0; i < octaves; i++)
        {
            valueSum += Calculate2D(x * freq, y * freq) * amp;
            maxVal += amp;
            amp *= persistence;
            freq *= 2;
        }

        // return float in range [0, 1]: sum / maximum possible value
        return valueSum / maxVal;
    }

    private float Calculate2D(int x, int y)
    {
        // Unit square values and distance
        int x0, x1, y0, y1;
        float xf, yf, u, v, dx0, dx1;
        int[] k0, k1;

        x0 = Mathf.FloorToInt((float)x / gridSize) * gridSize;
        x1 = x0 + gridSize;
        xf = (float)(x - x0) / gridSize;
        y0 = Mathf.FloorToInt((float)y / gridSize) * gridSize;
        y1 = y0 + gridSize;
        yf = (float)(y - y0) / gridSize;

        // Apply smoothstep
        u = Fade(xf);
        v = Fade(yf);

        // Take 'random' unit gradients and interpolate on x then y on unit square
        k0 = Gradient2D((int)(rng.UnitFloat(x0, y0) * period));
        k1 = Gradient2D((int)(rng.UnitFloat(x1, y0) * period));
        dx0 = Lerp(Dot(new float[2] { xf, yf }, k0), Dot(new float[2] { xf-1, yf }, k1), u);

        k0 = Gradient2D((int)(rng.UnitFloat(x0, y1) * period));
        k1 = Gradient2D((int)(rng.UnitFloat(x1, y1) * period));
        dx1 = Lerp(Dot(new float[2] { xf, yf - 1 }, k0), Dot(new float[2] { xf - 1, yf - 1 }, k1), u);

        // interpolate on y, set value in range [0 - 1]
        return (Lerp(dx0, dx1, v) * 1.5f + 1) / 2;
    }

    // Smoothstep (fade) function: 6t^5 - 15t^4 + 10t^3
    private float Fade(float t) { return t * t * t * (t * (t * 6 - 15) + 10); }

    private int[] Gradient2D(int v)
    {   
        int[] g = new int[2];
        g[0] = gradient[v, 0];
        g[1] = gradient[v, 1];

        return g;
    }

    private float Dot(float[] a, int[] b) { return a[0] * b[0] + a[1] * b[1]; }

    private float Lerp(float v0, float v1, float t) { return (1 - t) * v0 + t * v1; }
}

public class PseudoRandomGenerator
{
    // Permutation array and its period
    private readonly int[] permutation = {
        151, 160, 137, 91, 90, 15, 131, 13, 201, 95,
        96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69,
        142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148,
        247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219,
        203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149,
        56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74,
        165, 71, 134, 139, 48, 27, 166, 77, 146, 158,
        231, 83, 111, 229, 122, 60, 211, 133, 230, 220,
        105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54,
        65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132,
        187, 208, 89, 18, 169, 200, 196, 135, 130, 116,
        188, 159, 86, 164, 100, 109, 198, 173, 186, 3,
        64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147,
        118, 126, 255, 82, 85, 212, 207, 206, 59, 227,
        47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170,
        213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153,
        101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19,
        98, 108, 110, 79, 113, 224, 232, 178, 185, 112,
        104, 218, 246, 97, 228, 251, 34, 242, 193, 238,
        210, 144, 12, 191, 179, 162, 241, 81, 51, 145,
        235, 249, 14, 239, 107, 49, 192, 214, 31, 181,
        199, 106, 157, 184, 84, 204, 176, 115, 121, 50,
        45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114,
        67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215,
        61, 156, 180};

    private readonly int period;

    private readonly int[] numArray;

    // Constructor
    public PseudoRandomGenerator(int seed)
    {
        period = permutation.Length;
        numArray = new int[period];

        int a, b;
        a = (int)((float)seed / period);
        b = Mod(seed, period);

        for (int i = 0; i < period; i++)
        {
            numArray[i] = Mod(permutation[i] + a + b, period);
        }
    }

    // Return a bound float between [0, 1]
    public float UnitFloat(int x)
    {
        // Permutation values
        int a, b;
        x += period;
        a = (int)((float)x / period);
        b = Mod(x, period);

        return (float)(numArray[Mod(x, period)] + numArray[Mod((x * a), period)] + numArray[Mod((x + b), period)]) / (period * 3);
    }

    // Return a bound float between [0, 1]
    public float UnitFloat(int x, int y)
    {
        // Permutation values
        int a, b, dx;
        float dy;

        x += period;
        a = (int)((float)x / period);
        b = Mod(x, period);

        dx = (numArray[Mod(x, period)] + numArray[Mod((x * a), period)] + numArray[Mod((x + b), period)]);

        // Second-step permutation values
        y += dx;
        a = (int)((float)y / period);
        b = Mod(y, period);

        dy = (float)(numArray[Mod(y, period)] + numArray[Mod((y * a), period)] + numArray[Mod((y + b), period)]) / (period * 3);

        return dy;
    }

    // Return an integer in the range [0, n]
    public int IntVal(int x, int y, int n)
    {
        return (int)(UnitFloat(x, y) * n);
    }

    private int Mod(int a, int b)
    {
        if (a < 0) { return (a % b + b) % b; }
        else { return a % b; }
    }
}