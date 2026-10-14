using System.GameSession;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace System.Service
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
            new ThreeDayGameService().Validate(data);
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
            await File.WriteAllTextAsync(SystemPath, encrypted);
        }

        public static async UniTask<GamePlayerData> Load(string slot = "1")
        {
            var path = SavePath(slot);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("保存データがありません。", path);
            }

            var encrypted = await File.ReadAllTextAsync(path);
            var json = await Decrypt(encrypted);
            var data = JsonUtility.FromJson<GamePlayerData>(json);
            new ThreeDayGameService().Validate(data);
            return data;
        }

        public static async UniTask<GameSystemConfig> LoadSystem()
        {
            if (!File.Exists(SystemPath))
                return new GameSystemConfig();

            var encrypted = await File.ReadAllTextAsync(SystemPath);
            var json = await Decrypt(encrypted);
            return JsonUtility.FromJson<GameSystemConfig>(json);
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