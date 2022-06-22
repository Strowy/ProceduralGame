using System;
using AIR.Flume;
using Application.Interfaces;
using Domain;
using UnityEngine;

namespace Infrastructure.Runtime
{
    public class TerrainService : Dependent, ITerrainService
    {
        private int _maxHeight;
        private float[,] _terrainModifiers;
        private int _biomeSize;
        private IntegerPoint _lastBiome;

        private IHeightSource _heightSource;
        private IPropertiesService _propertiesService;
        private IValueSourceService _valueSourceService;
        private IValueSource _valueSource;

        public TerrainInfo GetTerrainData(int x, int y)
        {
            var cellData = CellData(x, y);
            return new TerrainInfo
            {
                Height = cellData[0],
                Zone = cellData[1],
                Prop = (Prop) cellData[2]
            };
        }

        public IntegerPoint GetBiome(int x, int y)
        {
            var bx = Mathf.FloorToInt((float) x / _biomeSize);
            var by = Mathf.FloorToInt((float) y / _biomeSize);
            return new IntegerPoint(x, y);
        }

        public void Inject(
            IHeightSource heightSource,
            IPropertiesService propertiesService,
            IValueSourceService valueSourceService)
        {
            _heightSource = heightSource;
            _valueSourceService = valueSourceService;
            _valueSource = valueSourceService.GetNewValueSource(1);
            _propertiesService = propertiesService;
            _lastBiome = IntegerPoint.Zero;
            SetTerrainModifiers();
        }

        private void SetTerrainModifiers()
        {
            var terrainProperties = _propertiesService.TerrainProperties;
            _maxHeight = terrainProperties.MaxHeight;
            _biomeSize = terrainProperties.BiomeSize;

            _terrainModifiers = new float[8, 3];
            // Height boundaries of each terrain type
            _terrainModifiers[0, 0] = 0;
            _terrainModifiers[1, 0] = 0.28f;
            _terrainModifiers[2, 0] = 0.30f;
            _terrainModifiers[3, 0] = 0.35f;
            _terrainModifiers[4, 0] = 0.45f;
            _terrainModifiers[5, 0] = 0.65f;
            _terrainModifiers[6, 0] = 0.75f;
            _terrainModifiers[7, 0] = 0.90f;
            // Height multipliers of each terrain type
            _terrainModifiers[0, 1] = 0;
            _terrainModifiers[1, 1] = 0;
            _terrainModifiers[2, 1] = 0.25f;
            _terrainModifiers[3, 1] = 0.50f;
            _terrainModifiers[4, 1] = 0.25f;
            _terrainModifiers[5, 1] = 1.00f;
            _terrainModifiers[6, 1] = 3.00f;
            _terrainModifiers[7, 1] = 1.00f;
            // Sum (b - a) * n where a, b are the lower, upper bounds of each terrain type
            _terrainModifiers[0, 2] = 0;
            for (var i = 1; i < 8; i++)
            {
                _terrainModifiers[i, 2] = _terrainModifiers[i - 1, 1]
                                          * (_terrainModifiers[i, 0] - _terrainModifiers[i - 1, 0])
                                          + _terrainModifiers[i - 1, 2];
            }
        }

        private static int SeedFromPosition(IntegerPoint position)
        {
            return Math.Abs(position.X * (position.Y + 13)) % 16384;
        }

        private void TrySetBiomeValueSource(int x, int y)
        {
            var currentBiome = GetBiome(x, y);
            if (currentBiome == _lastBiome)
                return;

            _valueSource = _valueSourceService.GetNewValueSource(SeedFromPosition(currentBiome));
            _lastBiome = currentBiome;
        }

        // Main function to produce map cell information. Returns [height, terrain type, special data]
        private int[] CellData(int x, int y)
        {
            TrySetBiomeValueSource(x, y);

            int[] g = { 0, 0, 0, 0 };
            float hVal;
            Tuple spc;
            int n = 8; // 8 types of base terrain

            // Get noise map value
            hVal = _heightSource.GetUnitHeight(x, y);

            // Get terrain type then calculate height and other values from that
            g[1] = TerrainType(hVal, n);
            // Special terrain type check
            spc = SpecialTerrainType(x, y, g[1], _lastBiome.X, _lastBiome.Y);
            g[2] = spc.a;
            g[3] = spc.b;
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

            dx = x - (bx * _biomeSize);
            dy = y - (by * _biomeSize);

            // If water terrain
            if (tType == 0 || tType == 1)
            {
                r.a = 1;
            }

            // Check for Dungeon Portals (only appear on certain terrain)
            if (tType == 4)
            {
                // Make it so it cannot appear on border cells of biome
                de = IntVal(bx, by, (_biomeSize - 4) * (_biomeSize - 4));
                de = (Mod(de, _biomeSize - 4) + 2) + (Mathf.FloorToInt((float) de / (_biomeSize - 4)) + 2) * _biomeSize;

                if (dx + dy * _biomeSize == de)
                {
                    r.a = 2;
                }
            }

            // Check for Dungeon Surroundings
            if ((tType == 3 || tType == 4 || tType == 5) && (r.a != 2))
            {
                int ex, ey;

                // Make it so portal cannot appear on border cells of biome (otherwise walls would cut off)
                de = IntVal(bx, by, (_biomeSize - 4) * (_biomeSize - 4));
                de = (Mod(de, _biomeSize - 4) + 2) + (Mathf.FloorToInt((float) de / (_biomeSize - 4)) + 2) * _biomeSize;
                ex = Mod(de, _biomeSize);
                ey = Mathf.FloorToInt((float) de / _biomeSize);

                // Creates a wall in the eight spaces around the dungeon portal
                if ((Mathf.Abs(dx - ex) < 3) && (Mathf.Abs(dy - ey) < 3))
                {
                    float hVal = _heightSource.GetUnitHeight(x - (dx - ex), y - (dy - ey));
                    if (TerrainType(hVal, 8) == 4)
                    {
                        r.a = 3;
                        r.b = TerrainHeight(hVal, new int[4] { 0, 4, 2, 0 });

                        if (((Mathf.Abs(dx - ex) > 0) && (Mathf.Abs(dy - ey) > 1)) ||
                            ((Mathf.Abs(dx - ex) > 1) && (Mathf.Abs(dy - ey) > 0)))
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
                if (height > _terrainModifiers[i, 0])
                {
                    r = i;
                }
            }

            return r;
        }

        private int TerrainHeight(float height, int[] vals)
        {
            int r = 0;

            if (vals[2] < 3) // Unmodified height values
            {
                r = (int) (((height - _terrainModifiers[vals[1], 0]) * _terrainModifiers[vals[1], 1] +
                            _terrainModifiers[vals[1], 2]) * _maxHeight);
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
            return (int) (_valueSource.UnitFloat(x, y) * n);
        }

        private int Mod(int a, int b)
        {
            if (a < 0)
            {
                return (a % b + b) % b;
            }
            else
            {
                return a % b;
            }
        }

        struct Tuple
        {
            public int a;
            public int b;

            public Tuple(int da, int db)
            {
                a = da;
                b = db;
            }
        }
    }
}