namespace Application.Interfaces
{
    public interface ISeedService
    {
        public int Seed { get; }
        public void SetSeed(int seed);
    }
}