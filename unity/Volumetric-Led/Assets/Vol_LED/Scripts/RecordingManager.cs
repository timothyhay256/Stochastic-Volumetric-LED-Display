using UnityEngine;
using System.IO;

public class FileCreator : MonoBehaviour
{
    // Public boolean variable to toggle file creation/removal
    public bool recording = false;
    public bool recordingByte = false;
    public bool exportByteData = false;
    // File path to create/remove the file (adjust as per your needs)
    private string filePath;
    private string filePathByte;
    private string filePathExport;

    private void Start()
    {
        filePath = Path.Combine("/tmp/", "start_animate");
        filePathByte = Path.Combine("/tmp/", "start_animate_byte");
        filePathExport = Path.Combine("/tmp/", "export_byte");
    }

    private void Update()
    {
        // Check if createFile boolean has changed
        if (recording || recordingByte)
        {
            if (!File.Exists(filePath))
            {
                if (recording) {
                    using (StreamWriter writer = File.CreateText(filePath))
                    {
                        writer.WriteLine("This file is used as a flag to let Unity communicate with the Volumetric LED software");
                    }
                    Debug.Log("File created: " + filePath);
                } else if (recordingByte) {
                    using (StreamWriter writer = File.CreateText(filePathByte))
                    {
                        writer.WriteLine("This file is used as a flag to let Unity communicate with the Volumetric LED software");
                    }
                    Debug.Log("File created: " + filePath);
                }
            }
        }
        if (!recording)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log("File deleted: " + filePath);
            }
        }
        if (!recordingByte)
        {
            if (File.Exists(filePathByte))
            {
                File.Delete(filePathByte);
                Debug.Log("File deleted: " + filePathByte);
            }
        }

        if (exportByteData) {
            exportByteData = false;
            if (!File.Exists(filePathExport)) {
                using (StreamWriter writer = File.CreateText(filePathExport))
                {
                    writer.WriteLine("This file is used as a flag to let Unity communicate with the Volumetric LED software");
                }
                Debug.Log("File created: " + filePath);
            }
        }
    }

}
