using AIR.Flume;
using Application.Interfaces;

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
                ;
        }
    }
}