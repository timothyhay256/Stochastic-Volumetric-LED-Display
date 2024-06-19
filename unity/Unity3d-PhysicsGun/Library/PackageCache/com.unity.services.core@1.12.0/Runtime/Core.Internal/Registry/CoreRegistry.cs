using System.Collections.Generic;
using NotNull = JetBrains.Annotations.NotNullAttribute;
using SuppressMessage = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;

namespace Unity.Services.Core.Internal
{
    /// <summary>
    /// A container to store all available <see cref="IServiceComponent"/> and <see cref="IService"/> in the project.
    /// </summary>
    public sealed class CoreRegistry
    {
        /// <summary>
        /// Get the main registry of this project.
        /// </summary>
        public static CoreRegistry Instance { get; internal set; }

        /// <summary>
        /// The unique identifier of the instance.
        /// </summary>
        public string InstanceId { get; }

        internal ServicesType Type { get; private set; }

        internal InitializationOptions Options { get; set; }

        [NotNull]
        internal IPackageRegistry PackageRegistry { get; private set; }

        [NotNull]
        internal IComponentRegistry ComponentRegistry { get; private set; }

        [NotNull]
        internal IServiceRegistry ServiceRegistry { get; private set; }

        internal CoreRegistry()
        {
            Type = ServicesType.Default;
            InstanceId = null;

            PackageRegistry = new PackageRegistry(new DependencyTree());
            ComponentRegistry = new ComponentRegistry();
            ServiceRegistry = new ServiceRegistry();
        }

        internal CoreRegistry(IPackageRegistry packageRegistry, ServicesType type = ServicesType.Default, string instanceId = null)
        {
            Type = type;
            InstanceId = instanceId;

            PackageRegistry = packageRegistry;
            ComponentRegistry = new ComponentRegistry();
            ServiceRegistry = new ServiceRegistry();
        }

        /// <summary>
        /// Store the given <paramref name="package"/> in this registry.
        /// </summary>
        /// <param name="package">
        /// The service package instance to register.
        /// </param>
        /// <typeparam name="TPackage">
        /// The type of <see cref="IInitializablePackage"/> to register.
        /// </typeparam>
        /// <returns>
        /// Return a handle to the registered <paramref name="package"/>
        /// to define its dependencies and provided components.
        /// </returns>
        public CoreRegistration RegisterPackage<TPackage>(
            [NotNull] TPackage package)
            where TPackage : IInitializablePackage
        {
            return PackageRegistry.RegisterPackage(package);
        }

        /// <summary>
        /// Store the given <paramref name="component"/> in this registry.
        /// </summary>
        /// <param name="component">
        /// The component instance to register.
        /// </param>
        /// <typeparam name="TComponent">
        /// The type of <see cref="IServiceComponent"/> to register.
        /// </typeparam>
        [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
        public void RegisterServiceComponent<TComponent>(
            [NotNull] TComponent component)
            where TComponent : IServiceComponent
        {
            ComponentRegistry.RegisterServiceComponent<TComponent>(component);
        }

        /// <summary>
        /// Get the instance of the given <see cref="IServiceComponent"/> type.
        /// </summary>
        /// <typeparam name="TComponent">
        /// The type of <see cref="IServiceComponent"/> to get.
        /// </typeparam>
        /// <returns>
        /// Return the instance of the given <see cref="IServiceComponent"/> type if it has been registered;
        /// throws an exception otherwise.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the requested type of <typeparamref name="TComponent"/> hasn't been registered yet.
        /// </exception>
        public TComponent GetServiceComponent<TComponent>()
            where TComponent : IServiceComponent
        {
            return ComponentRegistry.GetServiceComponent<TComponent>();
        }

        /// <summary>
        /// Get the instance of the given <see cref="IServiceComponent"/> type and if it is registered.
        /// </summary>
        /// <typeparam name="TComponent">
        /// The type of <see cref="IServiceComponent"/> to get.
        /// </typeparam>
        /// <param name="component">The component instance or the default value</param>
        /// <returns>If the component was found in the registry.</returns>
        public bool TryGetServiceComponent<TComponent>(out TComponent component)
            where TComponent : IServiceComponent
        {
            return ComponentRegistry.TryGetServiceComponent<TComponent>(out component);
        }

        /// <summary>
        /// Store the given service in this registry.
        /// </summary>
        /// <typeparam name="T">
        /// The type of <see cref="IService"/> to register.
        /// </typeparam>
        /// <param name="service">
        /// The service instance to register.
        /// </param>
        public void RegisterService<T>([NotNull] T service) where T : IService
        {
            ServiceRegistry.RegisterService(service);
        }

        /// <summary>
        /// Returns the given service in this registry.
        /// </summary>
        /// <typeparam name="T">
        /// The type of <see cref="IService"/> to get.
        /// </typeparam>
        /// <returns>
        /// Return the instance of the given <see cref="IService"/> type if it has been registered;
        /// returns null otherwise.
        /// </returns>
        public T GetService<T>() where T : IService
        {
            return ServiceRegistry.GetService<T>();
        }

        internal void LockComponentRegistration()
        {
            if (ComponentRegistry is LockedComponentRegistry)
            {
                return;
            }

            ComponentRegistry = new LockedComponentRegistry(ComponentRegistry);
        }

        internal void LockServiceRegistration()
        {
            if (ServiceRegistry is LockedServiceRegistry)
            {
                return;
            }

            ServiceRegistry = new LockedServiceRegistry(ServiceRegistry);
        }
    }
}
