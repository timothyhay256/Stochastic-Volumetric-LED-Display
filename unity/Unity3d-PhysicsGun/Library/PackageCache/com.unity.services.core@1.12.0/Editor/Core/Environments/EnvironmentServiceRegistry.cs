using Unity.Services.Core.Editor.Environments.Analytics;
using Unity.Services.Core.Editor.Environments.Save;
using Unity.Services.Core.Editor.Shared.DependencyInversion;

namespace Unity.Services.Core.Editor.Environments
{
    class EnvironmentServiceRegistry : AbstractRuntimeServices<EnvironmentServiceRegistry>
    {
        public EnvironmentServiceRegistry()
        {
            Initialize(new ServiceCollection());
        }

        public override void Register(ServiceCollection collection)
        {
            collection.RegisterSingleton(Factories.Default<IEnvironmentsApi, EnvironmentsApiInternal>);

            collection.Register(_ => new AccessTokens());
            collection.Register(Factories.Default<IAccessTokens, AccessTokens>);

            collection.RegisterSingleton(Factories.Default<IEnvironmentService, EnvironmentService>);
            collection.RegisterSingleton(Factories.Default<IEnvironmentProvider, EnvironmentProvider>);

            collection.Register(Factories.Default<IFileSystem, FileSystem>);
            collection.Register(Factories.Default<IEnvironmentSaveSystem, EnvironmentSaveSystem>);
            collection.Register(Factories.Default<IEnvironmentValidator, EnvironmentValidator>);
            collection.Register(Factories.Default<IEnvironmentAnalytics, EnvironmentAnalytics>);
            collection.Register(Factories.Default<IEnvironmentFetcher, EnvironmentsClientFactory>);
            collection.Register(Factories.Default<IProjectInfo, ProjectInfo>);
            collection.Register(Factories.Default<IAnalyticsSender, EnvironmentAnalyticsSender>);
        }
    }
}
