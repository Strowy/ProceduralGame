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
                .Register<ISeedService, SeedService>();
        }
    }
}