using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace NatureBears.Save
{
    /// <summary>
    /// AES-256-CBC encryption for the local save file. A fresh random IV is
    /// generated per save and prepended to the ciphertext.
    ///
    /// Key derivation: PBKDF2(constant secret + deviceUniqueIdentifier).
    /// Tradeoff — the device-bound key stops players from copying edited save
    /// files between devices, but iOS's deviceUniqueIdentifier can change after
    /// uninstall/reinstall, making the LOCAL file unrecoverable. That is
    /// acceptable: cross-device/reinstall recovery goes through
    /// ICloudSaveProvider as plain JSON, re-encrypted on the new device.
    /// (A constant-only key would be portable but trivially extractable from
    /// the app binary.)
    /// </summary>
    internal static class SaveCrypto
    {
        // Obfuscation, not secrecy — anyone decompiling the binary can find this.
        // Real protection for competitive/economy-critical data is server-side.
        private const string ConstSecret = "NB-Exp3d1t10n-H0ney&Embers";

        private static readonly byte[] FixedSalt =
        {
            0x4E, 0x61, 0x74, 0x75, 0x72, 0x65, 0x42, 0x65,
            0x61, 0x72, 0x73, 0x21, 0x53, 0x61, 0x6C, 0x74
        };

        private static byte[] _cachedKey;

        private static byte[] GetKey()
        {
            if (_cachedKey != null)
                return _cachedKey;

            // PBKDF2 is deliberately slow — derive once per session, never per save.
            string material = ConstSecret + SystemInfo.deviceUniqueIdentifier;
            using (var pbkdf2 = new Rfc2898DeriveBytes(material, FixedSalt, 10_000))
                _cachedKey = pbkdf2.GetBytes(32);

            return _cachedKey;
        }

        internal static byte[] Encrypt(string json)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = GetKey();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var encryptor = aes.CreateEncryptor())
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] plain = Encoding.UTF8.GetBytes(json);
                        cs.Write(plain, 0, plain.Length);
                        cs.FlushFinalBlock();
                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>Returns the decrypted JSON, or throws on corrupt/foreign data (caller treats as new game).</summary>
        internal static string Decrypt(byte[] blob)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = GetKey();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                int ivLength = aes.BlockSize / 8; // 16
                if (blob == null || blob.Length <= ivLength)
                    throw new InvalidDataException("Save blob too short.");

                byte[] iv = new byte[ivLength];
                Buffer.BlockCopy(blob, 0, iv, 0, ivLength);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(blob, ivLength, blob.Length - ivLength))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
