using AIR.Flume;
using Application.Interfaces;
using Infrastructure.Runtime.Terrain;

namespace Infrastructure.Runtime
{
    public class TestServiceInstaller : ServiceInstaller
    {
        protected override void InstallServices(FlumeServiceContainer container)
        {
            container
                .Register<IGameStateController, NullGameStateController>()
                .Register<IPropertiesService, PropertiesService>()
                .Register<IValueSourceService, PseudoRandomSourceService>()
                .Register<ISeedService, SeedService>()
                .Register<IHeightSource, PerlinNoiseGenerator>()
                .Register<ITerrainService, TerrainService>()
                ;
        }
    }
}