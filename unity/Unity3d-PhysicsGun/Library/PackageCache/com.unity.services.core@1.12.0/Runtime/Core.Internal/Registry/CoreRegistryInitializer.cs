using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NotNull = JetBrains.Annotations.NotNullAttribute;

namespace Unity.Services.Core.Internal
{
    /// <summary>
    /// Helper object to initialize all <see cref="IInitializablePackage"/> registered in a <see cref="CoreRegistry"/>.
    /// </summary>
    class CoreRegistryInitializer
    {
        [NotNull]
        readonly CoreRegistry m_Registry;

        [NotNull]
        readonly List<int> m_SortedPackageTypeHashes;

        public CoreRegistryInitializer([NotNull] CoreRegistry registry, [NotNull] List<int> sortedPackageTypeHashes)
        {
            m_Registry = registry;
            m_SortedPackageTypeHashes = sortedPackageTypeHashes;
        }

        public async Task<List<PackageInitializationInfo>> InitializeRegistryAsync()
        {
            var packagesInitInfos = new List<PackageInitializationInfo>(m_SortedPackageTypeHashes.Count);
            if (m_SortedPackageTypeHashes.Count <= 0)
            {
                return packagesInitInfos;
            }

            var dependencyTree = m_Registry.PackageRegistry.Tree;
            if (dependencyTree is null)
            {
                var inner = new NullReferenceException("Registry requires a valid dependency tree to be initialized.");
                throw new ServicesInitializationException(
                    "Registry is in an invalid state (dependency tree is null) and can't be initialized.", inner);
            }

            m_Registry.ComponentRegistry.ResetProvidedComponents(dependencyTree.ComponentTypeHashToInstance);
            var failureReasons = new List<Exception>(m_SortedPackageTypeHashes.Count);
            var stopwatch = new Stopwatch();
            for (var i = 0; i < m_SortedPackageTypeHashes.Count; i++)
            {
                var package = GetPackageAt(i);
                await TryInitializePackageAsync(package);
            }

            if (failureReasons.Count > 0)
            {
                Fail();
            }

            return packagesInitInfos;

            async Task TryInitializePackageAsync(IInitializablePackage package)
            {
                try
                {
                    stopwatch.Restart();
                    await InitializePackageAsync(package);

                    stopwatch.Stop();
                    var initializationInfo = new PackageInitializationInfo
                    {
                        PackageType = package.GetType(),
                        InitializationTimeInSeconds = stopwatch.Elapsed.TotalSeconds,
                    };
                    packagesInitInfos.Add(initializationInfo);
                }
                catch (Exception e)
                {
                    stopwatch.Stop();
                    failureReasons.Add(e);
                }
            }

            IInitializablePackage GetPackageAt(int index)
            {
                var packageTypeHash = m_SortedPackageTypeHashes[index];
                return dependencyTree.PackageTypeHashToInstance[packageTypeHash];
            }

            async Task InitializePackageAsync(IInitializablePackage package)
            {
                switch (m_Registry.Type)
                {
                    case ServicesType.Default:
                        await package.Initialize(m_Registry);
                        break;
                    case ServicesType.Instance:
                        if (package is IInitializablePackageV2)
                        {
                            var packageV2 = ((IInitializablePackageV2)package);
                            await packageV2.InitializeInstanceAsync(m_Registry);
                        }
                        break;
                }
            }

            void Fail()
            {
                const string errorMessage = "Some services couldn't be initialized."
                    + " Look at inner exceptions to get more information.";
                var innerException = new AggregateException(failureReasons);
                throw new ServicesInitializationException(errorMessage, innerException);
            }
        }
    }
}
