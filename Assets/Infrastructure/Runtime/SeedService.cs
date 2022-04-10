using Application.Interfaces;

namespace Infrastructure.Runtime
{
    public class SeedService : ISeedService
    {
        public int Seed { get; private set; } = 1;

        public void SetSeed(int seed)
            => Seed = seed;
    }
}