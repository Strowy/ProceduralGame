using Application.Interfaces;

namespace Infrastructure.Runtime
{
    public class PseudoRandomSourceService : IValueSourceService
    {
        public IValueSource GetNewValueSource(int seed)
            => new PseudoRandomSource(seed);
    }
}