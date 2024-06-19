using System;
using NotNull = JetBrains.Annotations.NotNullAttribute;

namespace Unity.Services.Core.Internal
{
    class LockedServiceRegistry : IServiceRegistry
    {
        const string k_ErrorMessage = "Service registration has been locked. " +
            "Make sure to register service services before all packages have finished initializing.";

        [NotNull]
        internal IServiceRegistry Registry { get; }

        public LockedServiceRegistry(
            [NotNull] IServiceRegistry registryToLock)
        {
            Registry = registryToLock;
        }

        public void RegisterService<T>(T service) where T : IService
        {
            throw new InvalidOperationException(k_ErrorMessage);
        }

        public T GetService<T>() where T : IService
        {
            return Registry.GetService<T>();
        }
    }
}
