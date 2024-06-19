using System.Threading.Tasks;

namespace Unity.Services.Core
{
    /// <summary>
    /// Central registry for an instance of unity services.
    /// </summary>
    public interface IUnityServices
    {
        /// <summary>
        /// The initialization state of the services instance.
        /// </summary>
        ServicesInitializationState State { get; }

        /// <summary>
        /// Initialize the services
        /// </summary>
        /// <param name="options">The options for the services</param>
        /// <returns></returns>
        Task InitializeAsync(InitializationOptions options = null);

        /// <summary>
        /// Retrieve a service from the service registry
        /// </summary>
        /// <typeparam name="T">The type that was registered for the service</typeparam>
        /// <returns>The service if available, otherwise null</returns>
        T GetService<T>() where T : IService;
    }
}
