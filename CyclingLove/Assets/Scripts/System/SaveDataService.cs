using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace System
{
    public static class SaveDataService
    {
        private const string EncryptionKey = "RGame2025_Key";
        private const string EncryptionIv = "RBG_IV_2025_Seed";

        private static string SavePath(string slot) => Application.persistentDataPath + $"/save_{slot}.dat";
        private static string SystemPath => Application.persistentDataPath + "/system.dat";


        public static async UniTaskVoid Save(GamePlayerData data, string slot = "1")
        {
            var json = JsonUtility.ToJson(data, true);
            var encrypted = await Encrypt(json);
            await File.WriteAllTextAsync(SavePath(slot), encrypted);
            Debug.Log($"[SaveSystem] Saved (encrypted) to {SavePath(slot)}");
        }

        public static async UniTaskVoid SaveSystem(GameSystemConfig data)
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
                Debug.LogWarning("[SaveSystem] No save file found, creating new data.");
                return new GamePlayerData();
            }

            var encrypted = await File.ReadAllTextAsync(path);
            var json = await Decrypt(encrypted);
            return JsonUtility.FromJson<GamePlayerData>(json);
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
            }, false, CancellationToken.None);
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