using Domain;

namespace Application.Interfaces
{
    public interface IValueSource
    {
        /// <summary>
        /// Reset the value source with the given seed value.
        /// </summary>
        /// <param name="seed">The seed that determines values given.</param>
        public void Reset(int seed);

        /// <summary>
        /// Returns the unit-range float for the given value and seed.
        /// </summary>
        /// <param name="x">Integer input</param>
        /// <returns>A unit-range float [0-1]</returns>
        public float UnitFloat(int x);

        /// <summary>
        /// Returns the unit-range float for the given (x,y) tuple and seed.
        /// </summary>
        /// <param name="x">x-value</param>
        /// <param name="y">y-value</param>
        /// <returns>A unit-range float [0,1]</returns>
        public float UnitFloat(int x, int y);

        /// <summary>
        /// Returns the unit-range float for the given cell and seed.
        /// </summary>
        /// <param name="integerPoint">The cell the value is from</param>
        /// <returns>A unit-range float [0-1]</returns>
        public float UnitFloat(IntegerPoint integerPoint);

        /// <summary>
        /// Returns a progressively-changing unit float; the sequence is always the same for the same seed.
        /// </summary>
        /// <returns>A float in the range [0, 1]</returns>
        public float NextUnitFloat();
    }
}