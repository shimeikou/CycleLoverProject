using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System;
using CyclingLove.Persistence;
using CyclingLove.Session;
using CyclingLove.State;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CyclingLove.Services
{
    public static class SaveDataService
    {
        private const string EncryptionKey = "RGame2025_Key";
        private const string EncryptionIv = "RBG_IV_2025_Seed";

        public static string SavePath(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot) || slot.Length > 64 ||
                !System.Text.RegularExpressions.Regex.IsMatch(slot, @"^[a-zA-Z0-9_-]+$"))
                throw new ArgumentException("Invalid save slot", nameof(slot));
            return Path.Combine(Application.persistentDataPath, $"save_{slot}.dat");
        }
        public static bool Exists(string slot) => File.Exists(SavePath(slot));
        private static string SystemPath => Application.persistentDataPath + "/system.dat";


        public static async UniTask Save(GamePlayerData data, string slot = "1")
        {
            PlayerSaveValidator.Validate(data);
            // Serialize on the main thread before awaiting, producing a consistent snapshot.
            var json = JsonUtility.ToJson(data, true);
            var encrypted = await Encrypt(json);
            var path = SavePath(slot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllTextAsync(temporary, encrypted);
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        public static async UniTask SaveSystem(GameSystemConfig data)
        {
            var json = JsonUtility.ToJson(data, true);
            var encrypted = await Encrypt(json);
            Directory.CreateDirectory(Path.GetDirectoryName(SystemPath));
            var temporary = SystemPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllTextAsync(temporary, encrypted);
                if (File.Exists(SystemPath)) File.Replace(temporary, SystemPath, SystemPath + ".bak");
                else File.Move(temporary, SystemPath);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        public static async UniTask<GamePlayerData> Load(string slot = "1")
        {
            var path = SavePath(slot);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("保存データがありません。", path);
            }

            try { return await LoadPlayerFile(path); }
            catch (Exception ex) when (IsDamagedSave(ex))
            {
                Debug.LogWarning($"Player save is damaged: {ex.GetType().Name}");
                var backup = path + ".bak";
                if (!File.Exists(backup)) throw;

                try { return await LoadPlayerFile(backup); }
                catch (Exception backupEx) when (IsDamagedSave(backupEx))
                {
                    Debug.LogWarning($"Player save backup is damaged: {backupEx.GetType().Name}");
                    throw new InvalidOperationException("保存データとバックアップを読み込めませんでした。", backupEx);
                }
            }
        }

        private static async UniTask<GamePlayerData> LoadPlayerFile(string path)
        {
            var encrypted = await File.ReadAllTextAsync(path);
            var json = await Decrypt(encrypted);
            var data = JsonUtility.FromJson<GamePlayerData>(json);
            PlayerSaveMigration.UpgradeToCurrent(data);
            PlayerSaveValidator.Validate(data);
            return data;
        }

        private static bool IsDamagedSave(Exception ex) =>
            ex is FormatException or CryptographicException or ArgumentException or InvalidOperationException;

        public static async UniTask<GameSystemConfig> LoadSystem()
        {
            if (!File.Exists(SystemPath))
                return new GameSystemConfig();

            try { return await LoadSystemFile(SystemPath); }
            catch (Exception ex) when (ex is FormatException or CryptographicException or ArgumentException)
            {
                Debug.LogWarning($"System settings are damaged: {ex.GetType().Name}");
                var backup = SystemPath + ".bak";
                if (!File.Exists(backup)) return new GameSystemConfig();
                try { return await LoadSystemFile(backup); }
                catch (Exception backupEx) when (backupEx is FormatException or CryptographicException or ArgumentException)
                {
                    Debug.LogWarning($"System settings backup is damaged: {backupEx.GetType().Name}");
                    return new GameSystemConfig();
                }
            }
        }

        private static async UniTask<GameSystemConfig> LoadSystemFile(string path)
        {
            var encrypted = await File.ReadAllTextAsync(path);
            var json = await Decrypt(encrypted);
            return JsonUtility.FromJson<GameSystemConfig>(json) ??
                   throw new ArgumentException("System settings JSON is empty");
        }

        private static async UniTask<string> Encrypt(string plainText)
        {
            return await UniTask.RunOnThreadPool(() =>
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32)[..32]);
                aes.IV = Encoding.UTF8.GetBytes(EncryptionIv.PadRight(16)[..16]);

                using MemoryStream ms = new();
        
                using (CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new(cs))
                    {
                        sw.Write(plainText); 
                    } 
                }
                
                return Convert.ToBase64String(ms.ToArray());
            }, true, CancellationToken.None);
        }

        private static async UniTask<string>  Decrypt(string cipherText)
        {
            var buffer = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32)[..32]);
            aes.IV = Encoding.UTF8.GetBytes(EncryptionIv.PadRight(16)[..16]);

            using MemoryStream ms = new(buffer);
            await using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            return await sr.ReadToEndAsync();
        }
    }
}
