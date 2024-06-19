using NotNull = JetBrains.Annotations.NotNullAttribute;

namespace Unity.Services.Core.Internal
{
    /// <summary>
    /// A container to store all available <see cref="IInitializablePackage"/> in the project.
    /// </summary>
    public sealed class CorePackageRegistry
    {
        /// <summary>
        /// Get the unique package registry of this project.
        /// </summary>
        public static CorePackageRegistry Instance
        {
            get;
            internal set;
        }

        internal IPackageRegistry Registry { get; set; }

        /// <summary>
        /// Constructor for package registry container
        /// </summary>
        internal CorePackageRegistry()
        {
            Registry = new PackageRegistry(new DependencyTree());
        }

        /// <summary>
        /// Constructor for package registry container
        /// </summary>
        /// <param name="registry">The registry</param>
        internal CorePackageRegistry(IPackageRegistry registry)
        {
            Registry = registry;
        }

        /// <summary>
        /// Register a package initializer to be executed during services initialization.
        /// </summary>
        /// <typeparam name="TPackage"></typeparam>
        /// <param name="package"></param>
        /// <returns></returns>
        public CoreRegistration Register<TPackage>(
            [NotNull] TPackage package)
            where TPackage : IInitializablePackage
        {
            return Registry.RegisterPackage(package);
        }

        internal void Lock()
        {
            if (Registry is LockedPackageRegistry)
            {
                return;
            }

            Registry = new LockedPackageRegistry(Registry);
        }
    }
}
