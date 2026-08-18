using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ShawarmaTycoon
{
    internal static class SaveProviderFactory
    {
        public static ISaveProvider CreateDefault()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new PlayerPrefsJsonSaveProvider();
#else
            return new JsonFileSaveProvider(Application.persistentDataPath);
#endif
        }
    }

    internal sealed class JsonFileSaveProvider : ISaveProvider
    {
        private readonly string path;
        private readonly string backupPath;

        public JsonFileSaveProvider(string directory)
        {
            path = Path.Combine(directory, "shawarma-tycoon.save.json");
            backupPath = path + ".bak";
        }

        public bool TryLoad(out SaveData data)
        {
            if (TryRead(path, out data)) return true;
            return TryRead(backupPath, out data);
        }

        public void Save(SaveData data)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, SaveCodec.Encode(data), Encoding.UTF8);
            try
            {
                if (File.Exists(path))
                    File.Replace(tempPath, path, backupPath, true);
                else
                    File.Move(tempPath, path);
            }
            catch (PlatformNotSupportedException)
            {
                FallbackReplace(tempPath);
            }
            catch (IOException)
            {
                FallbackReplace(tempPath);
            }
        }

        public void Delete()
        {
            DeleteIfPresent(path);
            DeleteIfPresent(backupPath);
            DeleteIfPresent(path + ".tmp");
        }

        private static bool TryRead(string candidate, out SaveData data)
        {
            data = null;
            if (!File.Exists(candidate)) return false;
            try
            {
                return SaveCodec.TryDecode(File.ReadAllText(candidate, Encoding.UTF8), out data);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Save] Could not read '{candidate}': {exception.Message}");
                return false;
            }
        }

        private void FallbackReplace(string tempPath)
        {
            if (File.Exists(path)) File.Copy(path, backupPath, true);
            File.Copy(tempPath, path, true);
            DeleteIfPresent(tempPath);
        }

        private static void DeleteIfPresent(string candidate)
        {
            if (File.Exists(candidate)) File.Delete(candidate);
        }
    }

    internal sealed class PlayerPrefsJsonSaveProvider : ISaveProvider
    {
        private const string SaveKey = "shawarma.tycoon.save.v2";
        private const string BackupKey = SaveKey + ".backup";

        public bool TryLoad(out SaveData data)
        {
            if (SaveCodec.TryDecode(PlayerPrefs.GetString(SaveKey, string.Empty), out data)) return true;
            return SaveCodec.TryDecode(PlayerPrefs.GetString(BackupKey, string.Empty), out data);
        }

        public void Save(SaveData data)
        {
            string current = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (!string.IsNullOrEmpty(current)) PlayerPrefs.SetString(BackupKey, current);
            PlayerPrefs.SetString(SaveKey, SaveCodec.Encode(data));
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(BackupKey);
            PlayerPrefs.Save();
        }
    }

    internal static class SaveCodec
    {
        public static string Encode(SaveData data)
        {
            string payload = JsonUtility.ToJson(data);
            SaveEnvelope envelope = new()
            {
                payload = payload,
                checksum = Checksum(payload)
            };
            return JsonUtility.ToJson(envelope);
        }

        public static bool TryDecode(string json, out SaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                SaveEnvelope envelope = JsonUtility.FromJson<SaveEnvelope>(json);
                if (envelope == null || string.IsNullOrEmpty(envelope.payload) ||
                    !string.Equals(envelope.checksum, Checksum(envelope.payload), StringComparison.Ordinal))
                    return false;

                data = JsonUtility.FromJson<SaveData>(envelope.payload);
                if (data == null) return false;
                data.Normalize();
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static string Checksum(string payload)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload ?? string.Empty));
            StringBuilder builder = new(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++) builder.Append(hash[i].ToString("x2"));
            return builder.ToString();
        }
    }
}
