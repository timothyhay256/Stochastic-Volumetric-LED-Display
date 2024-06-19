using System;
using System.Collections.Generic;
using NotNull = JetBrains.Annotations.NotNullAttribute;

namespace Unity.Services.Core.Internal
{
    class ServiceRegistry : IServiceRegistry
    {
        /// <summary>
        /// Key: Hash code of a <see cref="IService"/> type.
        /// Value: Service instance.
        /// </summary>
        [NotNull]
        internal Dictionary<int, IService> ServiceTypeHashToInstance { get; }

        public ServiceRegistry()
        {
            ServiceTypeHashToInstance = new Dictionary<int, IService>();
        }

        public ServiceRegistry([NotNull] Dictionary<int, IService> serviceTypeHashToInstance)
        {
            ServiceTypeHashToInstance = serviceTypeHashToInstance;
        }

        public void RegisterService<T>(T service) where T : IService
        {
            var serviceType = typeof(T);

            // This check is to avoid passing the service without specifying the interface type as a generic argument.
            if (service.GetType() == serviceType)
            {
                throw new ArgumentException("Interface type of service not specified.");
            }

            var serviceTypeHash = serviceType.GetHashCode();

            ServiceTypeHashToInstance[serviceTypeHash] = service;
        }

        public T GetService<T>() where T : IService
        {
            var serviceType = typeof(T);
            if (!ServiceTypeHashToInstance.TryGetValue(serviceType.GetHashCode(), out var service))
            {
                return default;
            }

            return (T)service;
        }
    }
}
