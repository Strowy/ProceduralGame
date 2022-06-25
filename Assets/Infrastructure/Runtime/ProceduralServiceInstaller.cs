using AIR.Flume;
using Application.Interfaces;
using Infrastructure.Runtime.Terrain;

namespace Infrastructure.Runtime
{
    public class ProceduralServiceInstaller : ServiceInstaller
    {
        protected override void InstallServices(FlumeServiceContainer container)
        {
            container
                .Register<IValueSourceService, PseudoRandomSourceService>()
                .Register<ISeedService, SeedService>()
                .Register<IPlayerService, PlayerService>()
                .Register<IGameStateController>(FindObjectOfType<WorldController>(true))
                .Register<IDungeonController>(FindObjectOfType<DungeonController>(true))
                .Register<IPropertiesService, PropertiesService>()
                .Register<IHeightSource, PerlinNoiseGenerator>()
                .Register<ITerrainService, TerrainService>()
                ;
        }
    }
}