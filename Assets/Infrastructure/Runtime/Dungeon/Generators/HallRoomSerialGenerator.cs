using System;
using System.Collections.Generic;
using AIR.Flume;
using Application.Interfaces;
using Domain;
using UnityEngine;

namespace Infrastructure.Runtime.Dungeon.Generators
{
    public class HallRoomSerialGenerator : Dependent, IDungeonGenerator
    {
        private const int FILLED = 0;
        private const int EMPTY = 1;
        private const int WALL = 2;
        private const int ENTRANCE = 3;
        private const int EXIT = 4;
        private const int MIN_SIDE_LENGTH = 3;
        private const int MIN_HALL_LENGTH = 2;

        private readonly List<RectBounds> _generatedRooms = new List<RectBounds>();
        private readonly Tuple<int, int> _roomSideLimits;
        private readonly Tuple<int, int> _hallLengthLimits;
        private readonly int _complexity;

        private IValueSourceService _valueSourceService;
        private IValueSource _valueSource;

        public DungeonMap Map { get; }

        public RectBounds MapBounds => Map.Bounds;

        public HallRoomSerialGenerator(int width, int height, int comp, int roomSize, int minTunnel, int maxTunnel)
        {
            Map = new DungeonMap(0, 0, width, height);
            _complexity = comp;
            _roomSideLimits = new Tuple<int, int>(roomSize * 2 + 1, roomSize * 2 + 1);
            _hallLengthLimits = new Tuple<int, int>(minTunnel, maxTunnel);

            Debug.Log($"Map dims: {Map.Bounds.ToString()}, Cell count: {Map.Cells.Length}");
        }

        public void Inject(IValueSourceService valueSourceService)
        {
            _valueSourceService = valueSourceService;
        }

        public void Generate(IntegerPoint entrancePosition)
        {
            _valueSource = _valueSourceService.GetNewValueSource(SeedFromPosition(entrancePosition));
            GenerateStartingArea();
            var attempts = 0;
            while (GenerateHallRoom() && attempts < _complexity)
                attempts++;

            BuildWalls();
            CreateAccessPoint(ENTRANCE);
            CreateAccessPoint(EXIT);
        }

        private void GenerateStartingArea()
        {
            var startPoint = GetBoundedRandomPoint(Map.GetBufferedBounds(5));
            var direction = Map.DirectionToClosestEdge(startPoint) * -1;
            GenerateRoom(startPoint, direction);
        }

        private bool GenerateHallRoom()
        {
            const int MAX_ATTEMPTS = 100;
            var attempts = 0;
            while (attempts < MAX_ATTEMPTS)
            {
                attempts++;
                var (any, start, direction) = GetTunnelPoint();
                if (!any)
                    return false;

                var length = RandInt(_hallLengthLimits.Item1, _hallLengthLimits.Item2);
                var (valid, point) = IsValidHall(start, direction, length);

                if (!valid) continue;
                if (!IsMinimumSpaceFree(point, direction)) continue;
                AddHallToMap(start, direction, length);
                GenerateRoom(point, direction);
                return true;
            }

            return false;
        }

        private void BuildWalls()
        {
            foreach (var mapCell in Map.MapCells())
            {
                if (IsBuildableWall(mapCell))
                    Map.SetValue(mapCell, WALL);
            }
        }

        private bool IsBuildableWall(IntegerPoint position)
        {
            if (Map.GetValue(position) != FILLED)
                return false;

            foreach (var point in NearNeighbours.FullEight)
            {
                if (!Map.WithinMap(position + point))
                    continue;

                if (Map.GetValue(position + point) == EMPTY)
                    return true;
            }

            return false;
        }

        private (bool, IntegerPoint) IsValidHall(IntegerPoint start, IntegerPoint direction, int length)
        {
            var end = start + direction * length;
            var hallRect = new RectBounds(start.X, start.Y, end.X, end.Y);
            return OverlapsExisting(hallRect)
                ? (false, IntegerPoint.Zero)
                : (true, end);
        }

        private (bool, IntegerPoint, IntegerPoint) GetTunnelPoint()
        {
            const int MAX_ATTEMPTS = 1000;
            var attempts = 0;
            while (attempts < MAX_ATTEMPTS)
            {
                attempts++;
                var (valid, position, direction) = SeekValidTunnelPoint();
                if (valid)
                    return (true, position, direction);
            }

            return (false, IntegerPoint.Zero, IntegerPoint.Zero);
        }

        private (bool, IntegerPoint, IntegerPoint) SeekValidTunnelPoint()
        {
            var sourceRoom = _generatedRooms[RandInt(0, _generatedRooms.Count - 1)];
            var position = new IntegerPoint(
                RandInt(sourceRoom.MinX, sourceRoom.MaxX),
                RandInt(sourceRoom.MinY, sourceRoom.MaxY));
            var adjacency = new RandomPatternAdjacency(_valueSource);
            var direction = adjacency.GetRandomDirection();
            while (Map.GetValue(position + direction) == EMPTY)
            {
                position = position + direction;
            }

            return IsClearableWall(position + direction, direction)
                ? (true, position + direction, direction)
                : (false, IntegerPoint.Zero, IntegerPoint.Zero);
        }

        private bool IsMinimumSpaceFree(IntegerPoint seedPoint, IntegerPoint entranceDirection)
            => !OverlapsExisting(MakeMinimumRoom(seedPoint, entranceDirection));

        private RectBounds MakeMinimumRoom(IntegerPoint seedPoint, IntegerPoint entranceDirection)
        {
            var min = seedPoint
                      + entranceDirection * new IntegerPoint(MIN_SIDE_LENGTH, MIN_SIDE_LENGTH)
                      + entranceDirection.Invert().Abs() * new IntegerPoint(1,1);
            return new RectBounds(min.X, min.Y, min.X + MIN_SIDE_LENGTH, min.Y + MIN_SIDE_LENGTH);
        }

        private void GenerateRoom(IntegerPoint seedPoint, IntegerPoint entranceDirection)
        {
            const int MAX_ATTEMPTS = 100;
            RectBounds tryRect;
            var attempts = 0;
            do
            {
                tryRect = PrototypeRoom(seedPoint, entranceDirection);
                attempts++;
            } while (OverlapsExisting(tryRect) && attempts < MAX_ATTEMPTS);

            if (attempts == MAX_ATTEMPTS)
                tryRect = MakeMinimumRoom(seedPoint, entranceDirection);
            AddRoomToMap(tryRect);
        }

        private void AddRoomToMap(RectBounds room)
        {
            _generatedRooms.Add(room);
            for (var xd = room.MinX; xd < room.MaxX + 1; xd++)
            {
                for (var yd = room.MinY; yd < room.MaxY + 1; yd++)
                {
                    Map.SetValue(new IntegerPoint(xd, yd), EMPTY);
                }
            }
        }

        private void AddHallToMap(IntegerPoint start, IntegerPoint direction, int length)
        {
            for (var i = 0; i < length; i++)
                Map.SetValue(start + direction * i, EMPTY);
        }

        private RectBounds PrototypeRoom(IntegerPoint seedPoint, IntegerPoint entranceDirection)
        {
            var width = RandInt(_roomSideLimits.Item1, _roomSideLimits.Item2);
            var height = RandInt(_roomSideLimits.Item1, _roomSideLimits.Item2);
            var anchor = new IntegerPoint(RandInt(0, width), RandInt(0, height));
            var min = seedPoint
                      + entranceDirection * new IntegerPoint(width, height)
                      + entranceDirection.Invert().Abs() * anchor;

            return new RectBounds(min.X, min.Y, min.X + width, min.Y + height);
        }

        private bool OverlapsExisting(RectBounds newRect)
        {
            if (!newRect.Overlaps(Map.GetBufferedBounds(1)))
                return false;

            foreach (var generatedRoom in _generatedRooms)
            {
                if (newRect.Overlaps(generatedRoom, 1))
                    return true;
            }

            return false;
        }

        private static int SeedFromPosition(IntegerPoint position)
        {
            return Math.Abs(position.X * (position.Y + 13)) % 16384;
        }

        private int RandInt(int minVal, int maxVal)
        {
            var diff = maxVal - minVal + 1;
            return (int) Math.Floor(minVal + diff * _valueSource.NextUnitFloat());
        }

        private IntegerPoint GetBoundedRandomPoint(RectBounds bounds)
        {
            var actualBounds = bounds.GetBufferlessOverlap(Map.GetBufferedBounds(1));
            return new IntegerPoint(
                RandInt(actualBounds.MinX, actualBounds.MaxX),
                RandInt(actualBounds.MinY, actualBounds.MaxY));
        }

        private bool IsClearableWall(IntegerPoint point, IntegerPoint direction)
        {
            if (Map.GetValue(point) != FILLED) return false;
            return Map.GetValue(point + direction.Invert()) == FILLED
                   && Map.GetValue(point - direction.Invert()) == FILLED;
        }

        private bool CreateAccessPoint(int accessType)
        {
            const int MAX_ATTEMPTS = 1000;
            var attempts = 0;
            while (attempts < MAX_ATTEMPTS)
            {
                attempts++;
                var point = GetBoundedRandomPoint(Map.GetBufferedBounds(5));
                if (!CheckSelfAndAllNeighboursAreEmpty(point))
                    continue;
                Map.SetValue(point, accessType);
                return true;
            }

            return false;
        }

        private bool CheckSelfAndAllNeighboursAreEmpty(IntegerPoint position)
        {
            if (!Map.WithinMap(position))
                return false;
            if (Map.GetValue(position) != EMPTY)
                return false;

            foreach (var neighbour in NearNeighbours.FullEight)
            {
                if (!Map.WithinMap(position + neighbour))
                    return false;
                if (Map.GetValue(position + neighbour) != EMPTY)
                    return false;
            }

            return true;
        }
    }
}