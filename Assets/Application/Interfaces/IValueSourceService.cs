namespace Application.Interfaces
{
    public interface IValueSourceService
    {
        /// <summary>
        /// Returns a new value source, initialized with the given seed.
        /// </summary>
        /// <param name="seed">The seed for the value source to use</param>
        /// <returns>New initialized instance of value source</returns>
        public IValueSource GetNewValueSource(int seed);
    }
}