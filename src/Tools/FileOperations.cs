using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zeffyr.Tools
{
    public static class FileOperations
    {
        private static File _fileInstance = new File();
        private static Directory _directoryInstance = new Directory();

        public static bool Exists(string path) => _fileInstance.FileExists(path);

        public static bool Remove(string path)
        {
            if (Exists(path))
            {
                Error error = _directoryInstance.Remove(path);
                return (error == Error.Ok);
            }
            return false;
        }
        
        public static bool TryWriteJson<T>(T data, string path, string pass = null)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    IgnoreReadOnlyProperties = true,
                    IgnoreReadOnlyFields = true,
                    Converters = { new JsonStringEnumConverter() }
                });

                if (pass != null)
                    _fileInstance.OpenEncryptedWithPass(path, File.ModeFlags.Write, pass);
                else
                    _fileInstance.Open(path, File.ModeFlags.Write);
                
                _fileInstance.StoreString(json);
                _fileInstance.Close();
                return true;
            }
            catch (Exception ex)
            {
                GD.PushError(ex.Message);
                return false;
            }
        }

        public static bool TryLoadJson<T>(string path, out T data, string pass = null)
        {
            try
            {
                if (!_fileInstance.FileExists(path))
                {
                    data = default(T);
                    return false;
                }
                
                if (pass != null)
                    _fileInstance.OpenEncryptedWithPass(path, File.ModeFlags.Read, pass);
                else
                    _fileInstance.Open(path, File.ModeFlags.Read);
                
                string json = _fileInstance.GetAsText();
                _fileInstance.Close();
                
                data = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions()
                {
                    Converters = { new JsonStringEnumConverter() }
                });
                return true;
            }
            catch (Exception ex)
            {
                GD.PushError(ex.Message);
                data = default(T);
                return false;
            }
        }
    }
}