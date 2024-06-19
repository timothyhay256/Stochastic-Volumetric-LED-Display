namespace Unity.Services.Core.Editor.Environments
{
    /// <summary>
    /// Class that grants access to the <see cref="IEnvironmentsApi"/> instance.
    /// </summary>
    public static class EnvironmentsApi
    {
        /// <summary>
        /// Instance of the <see cref="IEnvironmentsApi"/> class.
        /// </summary>
        public static IEnvironmentsApi Instance => EnvironmentServiceRegistry.Instance.GetService<IEnvironmentsApi>();
    }
}
