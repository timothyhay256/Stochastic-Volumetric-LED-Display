using System;
using System.Threading.Tasks;
using Unity.Services.Analytics;
using Unity.Services.Analytics.Data;
using Unity.Services.Analytics.Internal;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Analytics.Internal;
using Unity.Services.Core.Configuration.Internal;
using Unity.Services.Core.Device.Internal;
using Unity.Services.Core.Environments.Internal;
using Unity.Services.Core.Internal;
using UnityEngine;

class Ua2CoreInitializeCallback : IInitializablePackage
{
    const string k_CollectUrlPattern = "https://collect.analytics.unity3d.com/api/analytics/collect/v2/projects/{0}/environments/{1}";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        CoreRegistry.Instance.RegisterPackage(new Ua2CoreInitializeCallback())
            .DependsOn<IInstallationId>()
            .DependsOn<ICloudProjectId>()
            .DependsOn<IEnvironments>()
            .DependsOn<IExternalUserId>()
            .DependsOn<IProjectConfiguration>()
            .OptionallyDependsOn<IPlayerId>()
            .ProvidesComponent<IAnalyticsStandardEventComponent>();
    }

    public async Task Initialize(CoreRegistry registry)
    {
        var cloudProjectId = registry.GetServiceComponent<ICloudProjectId>();
        var installationId = registry.GetServiceComponent<IInstallationId>();
        var playerId = registry.GetServiceComponent<IPlayerId>();
        var environments = registry.GetServiceComponent<IEnvironments>();
        var customUserId = registry.GetServiceComponent<IExternalUserId>();

        var coreStatsHelper = new CoreStatsHelper();
        var consentTracker = new ConsentTracker(coreStatsHelper);

        string projectId = cloudProjectId?.GetCloudProjectId() ?? Application.cloudProjectId;
        string collectUrl = String.Format(k_CollectUrlPattern, projectId, environments.Current.ToLowerInvariant());

        var buffer = new BufferX(new BufferSystemCalls(), new DiskCache(new FileSystemCalls()));

        var containerObject = AnalyticsContainer.CreateContainer();
        var webRequestHelper = new WebRequestHelper();
        var dispatcher = new Dispatcher(webRequestHelper, collectUrl);

        AnalyticsService.internalInstance = new AnalyticsServiceInstance(
            new DataGenerator(),
            buffer,
            coreStatsHelper,
            consentTracker,
            dispatcher,
            new AnalyticsForgetter(collectUrl, new PlayerPrefsPersistence(), webRequestHelper),
            installationId,
            playerId,
            environments.Current,
            customUserId,
            new AnalyticsServiceSystemCalls(),
            containerObject);
        buffer.InjectIds(AnalyticsService.internalInstance);
        containerObject.Initialize(AnalyticsService.internalInstance);
        AnalyticsService.internalInstance.ResumeDataDeletionIfNecessary();

        StandardEventServiceComponent standardEventComponent = new StandardEventServiceComponent(
            registry.GetServiceComponent<IProjectConfiguration>(),
            AnalyticsService.internalInstance);
        registry.RegisterServiceComponent<IAnalyticsStandardEventComponent>(standardEventComponent);

        AnalyticsUserIdServiceComponent userIdComponent = new AnalyticsUserIdServiceComponent(AnalyticsService.internalInstance);
        registry.RegisterServiceComponent<IAnalyticsUserId>(userIdComponent);

#if UNITY_ANALYTICS_DEVELOPMENT
        Debug.LogFormat("Core Initialize Callback\nCollect URL: {0}\nInstall ID: {1}\nPlayer ID: {2}\nCustom Analytics ID: {3}",
            collectUrl,
            installationId.GetOrCreateIdentifier(),
            playerId?.PlayerId,
            customUserId.UserId
        );
#endif

        await Task.CompletedTask;
    }
}
