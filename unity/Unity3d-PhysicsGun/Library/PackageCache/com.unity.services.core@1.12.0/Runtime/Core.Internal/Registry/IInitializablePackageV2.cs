using System.Threading.Tasks;

namespace Unity.Services.Core.Internal
{
    /// <summary>
    /// For package inializers that supports multiple instances and identities.
    /// </summary>
    public interface IInitializablePackageV2 : IInitializablePackage
    {
        /// <summary>
        /// Registers itself to the core package registry.
        /// </summary>
        /// <param name="registry">The package registry to register to.</param>
        void Register(CorePackageRegistry registry);

        /// <summary>
        /// Initializes an instance of the service.
        /// </summary>
        /// <param name="registry">The core registry to retrieve from and register to.</param>
        /// <returns></returns>
        Task InitializeInstanceAsync(CoreRegistry registry);
    }
}
