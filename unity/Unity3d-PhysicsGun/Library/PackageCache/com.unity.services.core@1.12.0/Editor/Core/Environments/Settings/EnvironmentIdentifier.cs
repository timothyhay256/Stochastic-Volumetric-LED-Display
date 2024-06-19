namespace Unity.Services.Core.Editor.Settings
{
    struct EnvironmentIdentifier : IEditorGameServiceIdentifier
    {
        public string GetKey() { return "Environment"; } //NOSONAR
    }
}
