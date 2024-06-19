using System.IO;

namespace Unity.Services.Core.Editor
{
    class FileSystem : IFileSystem
    {
        public string GetOrCreateFileContent(string path)
        {
            var fileDirectoryName = Path.GetDirectoryName(path);
            if (!Directory.Exists(fileDirectoryName) && !string.IsNullOrEmpty(fileDirectoryName))
            {
                Directory.CreateDirectory(fileDirectoryName);
            }

            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            var fileStream = File.Create(path);
            fileStream.Close();
            return string.Empty;
        }

        public void SaveFile(string path, string fileContent)
        {
            File.WriteAllText(path, fileContent);
        }
    }
}
