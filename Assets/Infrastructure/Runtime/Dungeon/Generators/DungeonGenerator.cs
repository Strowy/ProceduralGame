using System;
using AIR.Flume;
using Application.Interfaces;
using Domain;

namespace Infrastructure.Runtime.Dungeon.Generators
{
    public class DungeonGenerator : Dependent, IDungeonGenerator
    {
        public DungeonMap Map { get; }

        private IValueSourceService _valueSourceService;
        private IValueSource _valueSource;

        // Max room size : max 2 * rSize + 1
        private readonly int _roomRadius;

        // Max tunnel size : range tSize[0] to tSize[0] + tSize[1]
        private readonly int[] _tunnelRange;

        // Complexity - essentially the number of attempts made to dig new tunnels/rooms
        private readonly int _complexity;

        // Constructor
        public DungeonGenerator(int width, int height, int comp, int roomSize, int minTunnel, int maxTunnel)
        {
            _complexity = comp;
            _roomRadius = roomSize;
            _tunnelRange = new[] { minTunnel, maxTunnel };

            Map = new DungeonMap(0, 0, width, height);
        }

        public void Inject(IValueSourceService valueSourceService)
        {
            _valueSourceService = valueSourceService;
        }

        public void Generate(IntegerPoint entrancePosition)
        {
            _valueSource = _valueSourceService.GetNewValueSource(SeedFromPosition(entrancePosition));
            Generate(entrancePosition.X, entrancePosition.Y);
        }

        private static int SeedFromPosition(IntegerPoint position)
        {
            return Math.Abs(position.X * (position.Y + 13)) % 16384;
        }

        // Main generation function: overwrites this.map with a new map
        //  Generates pseudo-randomly from an (x,y) input
        public void Generate(int x, int y)
        {
            Map.Clear();

            IntegerPoint point, dir, size, bounds;
            int len;

            // Generation process counters
            int tickCounter, failCounter, failsPerTick;

            // Generate seed room
            point = Map.Bounds.Dims / 2;
            bounds = point / 2;
            size = new IntegerPoint(_valueSource.RandInt(_roomRadius) + 1, _valueSource.RandInt(_roomRadius) + 1);

            point = SelectBoundedRandomPoint(point, bounds);
            CreateRoom(point, size);

            tickCounter = 0;
            failCounter = 0;
            failsPerTick = 10;

            // Generator loop
            do
            {
                // Find random room space
                do
                {
                    point = SelectRandomPoint();
                } while (Map.GetValue(point) != 1);

                // Test for valid adjacent wall
                dir = TestForValidWall(point);
                // On fail increment fail count, else attempt tunnel generation
                if (dir.X == 0 && dir.Y == 0)
                {
                    failCounter++;
                }
                else
                {
                    len = _valueSource.RandInt(_tunnelRange[1] + 1) + _tunnelRange[0];
                    // On creation fail increment fail count, else attempt room generation
                    if (!TestForValidTunnel(point + dir, dir, len))
                    {
                        failCounter++;
                    }
                    else
                    {
                        size = new IntegerPoint(_valueSource.RandInt(_roomRadius) + 1, _valueSource.RandInt(_roomRadius) + 1);
                        // On creation fail increment fail count, else increment success and finish creation
                        if (!TestForValidRoom(point + dir * len, size))
                        {
                            failCounter++;
                        }
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
            } while (tickCounter < _complexity);

            // Find and create access points
            int select = _valueSource.RandInt(4);
            int[,] quad = new int[4, 2]
            {
                { bounds.X, bounds.Y }, { bounds.X * 3, bounds.Y }, { bounds.X, bounds.Y * 3 },
                { bounds.X * 3, bounds.Y * 3 }
            };
            IntegerPoint pos;
            for (int i = 0; i < 2; i++)
            {
                // Switch to try to keep entrance / exit as far from each other as possible
                select = 3 - select;
                pos = new IntegerPoint(quad[select, 0], quad[select, 1]);

                int n = 0;
                do
                {
                    point = SelectBoundedRandomPoint(pos, bounds - 1);
                    n++;
                } while ((Map.GetValue(point) != 1 || !(TestForValidAccess(point))) && n < 1000);

                if (n >= 1000)
                {
                    do
                    {
                        point = SelectBoundedRandomPoint(pos, Map.Bounds.Dims - 2);
                    } while (Map.GetValue(point) != 1 || !(TestForValidAccess(point)));
                }

                CreateAccessPoint(point, i);
            }

            // Cleanup map and wallbound walkable areas
            for (var i = 0; i < Map.Cells.Length; i++)
            {
                if (Map.Cells[i] == 2)
                {
                    Map.Cells[i] = 1;
                }
            }

            for (int i = 0; i < Map.Bounds.Dims.X; i++)
            {
                for (int j = 0; j < Map.Bounds.Dims.Y; j++)
                {
                    if (Map.Cells[i + j * Map.Bounds.Dims.X] == 0)
                    {
                        var p = new IntegerPoint(i, j);
                        if (CheckConstruct(p) == true)
                        {
                            Map.Cells[i + j * Map.Bounds.Dims.X] = 2;
                        }
                    }
                }
            }
        }

        // Construct a wall if any of near neighbours is a room/tunnel space
        private bool CheckConstruct(IntegerPoint pos)
        {
            int k;
            // Map of checked spaces
            int[,] dir = new int[8, 2]
                { { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 } };

            k = 0;
            // Go through near neighbours, counting all spaces that are walkable
            for (int i = 0; i < dir.GetLength(0); i++)
            {
                int tx = pos.X + dir[i, 0];
                int ty = pos.Y + dir[i, 1];
                // If out of bounds, do not count
                if (!(tx < 0 || tx >= Map.Bounds.Dims.X || ty < 0 || ty >= Map.Bounds.Dims.Y))
                {
                    if (Map.Cells[tx + ty * Map.Bounds.Dims.X] == 1)
                    {
                        k++;
                    }
                }
            }

            // Wall constructable if any room spaces
            if (k == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        // Check surrounding 8 spaces of given position
        private bool CheckNearNeighbours(IntegerPoint pos)
        {
            int k;
            // Map of checked spaces
            int[,] dir = new int[8, 2]
                { { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 } };

            k = 0;
            // Go through near neighbours, counting all spaces that are not rooms
            for (int i = 0; i < dir.GetLength(0); i++)
            {
                int tx = pos.X + dir[i, 0];
                int ty = pos.Y + dir[i, 1];
                // If out of bounds, failure and considered a room
                if (!(tx < 0 || tx >= Map.Bounds.Dims.X || ty < 0 || ty >= Map.Bounds.Dims.Y))
                {
                    if (Map.Cells[tx + ty * Map.Bounds.Dims.X] != 1)
                    {
                        k++;
                    }
                }
            }

            // Position invalid if any room spaces
            if (k == 8)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Creates access point : Entrance (3) if chk is 0, else Exit (4)
        private void CreateAccessPoint(IntegerPoint pos, int chk)
        {
            if (chk == 0)
            {
                Map.Cells[pos.X + pos.Y * Map.Bounds.Dims.X] = 3;
            }
            else
            {
                Map.Cells[pos.X + pos.Y * Map.Bounds.Dims.X] = 4;
            }
        }

        // Creates a room centred on position with given size
        //  size = ([size of room] - 1) / 2: all rooms are of odd size in both dimensions
        private void CreateRoom(IntegerPoint pos, IntegerPoint size)
        {
            for (int i = pos.X - size.X; i <= pos.X + size.X; i++)
            {
                for (int j = pos.Y - size.Y; j <= pos.Y + size.Y; j++)
                {
                    Map.Cells[i + j * Map.Bounds.Dims.X] = 1;
                }
            }
        }

        // Creates a tunnel (1 width, len length) at given position in given direction
        private void CreateTunnel(IntegerPoint pos, IntegerPoint dir, int len)
        {
            for (int i = 0; i < len; i++)
            {
                Map.Cells[(pos.X + dir.X * i) + (pos.Y + dir.Y * i) * Map.Bounds.Dims.X] = 2;
            }
        }

        // Select random point on map within bounded range of a given point with edge buffer
        private IntegerPoint SelectBoundedRandomPoint(IntegerPoint pos, IntegerPoint bounds)
        {
            IntegerPoint point;

            // Buffer the point, but prevent inf. loop by poor bounding or position
            int n = 0;
            do
            {
                point = new IntegerPoint(
                    _valueSource.RandInt(2 * bounds.X + 1) + (pos.X - bounds.X),
                    _valueSource.RandInt(2 * bounds.Y + 1) + (pos.Y - bounds.Y));
                n++;
            } while ((point.X < 2 || point.X > Map.Bounds.Dims.X - 3 || point.Y < 2 || point.Y > Map.Bounds.Dims.Y - 3) && n < 1000);

            // Assign centre of map as default if broken due to loop count
            if (n >= 1000)
            {
                point = Map.Bounds.Dims / 2;
            }

            return point;
        }

        // Select random point on the map with 2 point edge buffer
        private IntegerPoint SelectRandomPoint()
        {
            return new IntegerPoint(
                _valueSource.RandInt(Map.Bounds.Dims.X - 4) + 2,
                _valueSource.RandInt(Map.Bounds.Dims.Y - 4) + 2);
        }

        // Test for valid access point (entrance / exit)
        private bool TestForValidAccess(IntegerPoint pos)
        {
            // Only the four cardinal directions are necessary as there are no concave rooms
            int[,] dir = new int[4, 2] { { -1, 0 }, { 0, -1 }, { 1, 0 }, { 0, 1 } };

            bool t = false;
            int i = 0;
            // floor test
            while (i < 4 && !t)
            {
                int tx = pos.X + dir[i, 0];
                int ty = pos.Y + dir[i, 1];

                // If any space is not floor, it's a failure
                if (Map.Cells[tx + ty * Map.Bounds.Dims.X] != 1)
                {
                    t = true;
                }

                i++;
            }

            // If i = 4 AND t is false all spaces were floors
            if (i == 4 && !t)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Test for valid room centered on position with given size
        private bool TestForValidRoom(IntegerPoint pos, IntegerPoint size)
        {
            int k;
            // Map of the checked spaces
            int[,] dir = new int[9, 2]
                { { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 }, { 0, 0 } };

            k = 0;
            // Check the 9 points (corners, midpoints, centre)
            for (int i = 0; i < dir.GetLength(0); i++)
            {
                var t = new IntegerPoint(pos.X + dir[i, 0] * size.X, pos.Y + dir[i, 1] * size.Y);
                if (!CheckNearNeighbours(t))
                {
                    k++;
                }
            }

            // If any failures, would overwrite another room, so a failure.
            if (k > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        // Test for valid tunnel starting at position in direction with given length
        private bool TestForValidTunnel(IntegerPoint pos, IntegerPoint dir, int len)
        {
            bool t = true;
            // Check the planned space is entirely filled in
            for (int i = 0; i < len; i++)
            {
                int tx = pos.X + dir.X * i;
                int ty = pos.Y + dir.Y * i;

                // Check buffer
                if (!(tx < 2 || tx > Map.Bounds.Dims.X - 3 || ty < 2 || ty > Map.Bounds.Dims.Y - 3))
                {
                    if (Map.Cells[tx + ty * Map.Bounds.Dims.X] != 0)
                    {
                        t = false;
                    }
                }
                else
                {
                    t = false;
                }
            }

            return t;
        }

        private IntegerPoint TestForValidWall(IntegerPoint pos)
        {
            var val = IntegerPoint.Zero;

            // The 4 spaces left, up, right, down are the relevant spaces to test
            //  however if not searched in randomized order, causes a directional bias in room generation
            int[,] dir = new int[4, 2] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };

            // Get (x,y) being -1 or 1, and another int to determine order, then generate test array
            int rx = _valueSource.RandInt(2);
            rx = (rx == 0) ? -1 : 1;
            int ry = _valueSource.RandInt(2);
            ry = (ry == 0) ? -1 : 1;
            int ro = _valueSource.RandInt(2);

            if (ro == 0)
            {
                dir[0, 1] += ry;
                dir[1, 0] += rx;
                dir[2, 1] -= ry;
                dir[3, 0] -= rx;
            }
            else
            {
                dir[0, 0] += rx;
                dir[1, 1] += ry;
                dir[2, 0] -= rx;
                dir[3, 1] -= ry;
            }

            bool t = false;
            int i = 0;
            // Wall test
            while (i < 4 && !t)
            {
                int tx = pos.X + dir[i, 0];
                int ty = pos.Y + dir[i, 1];
                // if wall, check adjacent spaces are also walls - if so, space is valid wall
                if (Map.Cells[tx + ty * Map.Bounds.Dims.X] == 0)
                {
                    if (Map.Cells[(tx + dir[i, 1]) + (ty + dir[i, 0]) * Map.Bounds.Dims.X] == 0 &&
                        Map.Cells[(tx - dir[i, 1]) + (ty - dir[i, 0]) * Map.Bounds.Dims.X] == 0)
                    {
                        val = new IntegerPoint(dir[i, 0], dir[i, 1]);
                        t = true;
                    }
                }

                i++;
            }

            // If i = 4 no valid walls were found
            if (i == 4)
            {
                val = IntegerPoint.Zero;
            }

            return val;
        }
    }
}