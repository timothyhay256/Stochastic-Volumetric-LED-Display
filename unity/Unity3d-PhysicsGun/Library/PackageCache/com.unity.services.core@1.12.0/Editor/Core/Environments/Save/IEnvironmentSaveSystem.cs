namespace Unity.Services.Core.Editor.Environments.Save
{
    interface IEnvironmentSaveSystem
    {
        void SaveEnvironment(string environment);
        string LoadEnvironment();
    }
}
