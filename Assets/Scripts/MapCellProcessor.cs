using AIR.Flume;
using Application.Interfaces;
using Infrastructure.Runtime;
using UnityEngine;

public class MapCellProcessor : Dependent
{
    private readonly int maxHeight;
    private readonly float[,] terrainModifiers;
    private readonly int biomeSize;

    private IValueSource _valueSource;
    private IHeightSource _heightSource;

    public MapCellProcessor(int seed, int mHeight, int bSize)
    {
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

    public void Inject(
        ISeedService seedService,
        IValueSourceService valueSourceService)
    {
        _heightSource = new PerlinNoiseGenerator();
        _valueSource = valueSourceService.GetNewValueSource(seedService.Seed);
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
        hVal = _heightSource.GetUnitHeight(x, y);

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
            de = IntVal(bx, by, (biomeSize - 4) * (biomeSize - 4));
            de = (Mod(de, biomeSize - 4) + 2) + (Mathf.FloorToInt((float)de / (biomeSize - 4)) + 2) * biomeSize;

            if (dx + dy * biomeSize == de) { r.a = 2; }
        }
        // Check for Dungeon Surroundings
        if ((tType == 3 || tType == 4 || tType == 5) && (r.a != 2))
        {
            int ex, ey;

            // Make it so portal cannot appear on border cells of biome (otherwise walls would cut off)
            de = IntVal(bx, by, (biomeSize - 4) * (biomeSize - 4));
            de = (Mod(de, biomeSize - 4) + 2) + (Mathf.FloorToInt((float)de / (biomeSize - 4)) + 2) * biomeSize;
            ex = Mod(de, biomeSize);
            ey = Mathf.FloorToInt((float)de / biomeSize);

            // Creates a wall in the eight spaces around the dungeon portal
            if ((Mathf.Abs(dx - ex) < 3) && (Mathf.Abs(dy - ey) < 3))
            {
                float hVal = _heightSource.GetUnitHeight(x - (dx - ex), y - (dy - ey));
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

    private int IntVal(int x, int y, int n)
    {
        return (int)(_valueSource.UnitFloat(x, y) * n);
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