namespace Unity.Services.Core.Editor
{
    interface IFileSystem
    {
        string GetOrCreateFileContent(string path);

        void SaveFile(string path , string fileContent);
    }
}
