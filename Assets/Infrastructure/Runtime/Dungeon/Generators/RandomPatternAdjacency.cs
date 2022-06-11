using System;
using System.Collections.Generic;
using Application.Interfaces;
using Domain;

namespace Infrastructure.Runtime.Dungeon.Generators
{
    public class RandomPatternAdjacency
    {
        private static readonly IntegerPoint[] Directions =
        {
            new IntegerPoint(-1, 0),
            new IntegerPoint(0, -1),
            new IntegerPoint(1, 0),
            new IntegerPoint(0, 1)
        };

        private int _rotationDirection;
        private int _startPoint;

        private IValueSource _valueSource;

        public RandomPatternAdjacency(IValueSource valueSource)
        {
            _valueSource = valueSource;
        }

        public IntegerPoint GetRandomDirection()
        {
            var index = (int) Math.Floor(_valueSource.NextUnitFloat() * Directions.Length);
            return Directions[index];
        }

        public IEnumerable<IntegerPoint> Cycle()
        {
            _startPoint = (int) Math.Floor(_valueSource.NextUnitFloat() * Directions.Length);
            _rotationDirection = _valueSource.NextUnitFloat() >= 0.5f ? -1 : 1;
            for (var id = 0; id < Directions.Length; id++)
            {
                yield return Directions[(_startPoint + id * _rotationDirection) % Directions.Length];
            }
        }
    }
}