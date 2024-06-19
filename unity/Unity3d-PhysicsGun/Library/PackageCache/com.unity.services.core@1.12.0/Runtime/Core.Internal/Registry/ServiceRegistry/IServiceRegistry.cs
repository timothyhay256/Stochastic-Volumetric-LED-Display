using NotNull = JetBrains.Annotations.NotNullAttribute;

namespace Unity.Services.Core.Internal
{
    interface IServiceRegistry
    {
        void RegisterService<T>([NotNull] T service) where T : IService;
        T GetService<T>() where T : IService;
    }
}
