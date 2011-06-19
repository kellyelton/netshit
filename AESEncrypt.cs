using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Skylabs.NetShit
{
    public static class AESEncryption
    {
        public static byte[] Encrypt(byte[] input, string Password, string Salt, string HashAlgorithm, int PasswordIterations, string InitialVector, int KeySize)
        {
            byte[] InitialVectorBytes = Convert.FromBase64String(InitialVector);
            byte[] SaltValueBytes = Convert.FromBase64String(Salt);
            byte[] PlainTextBytes =input;
            PasswordDeriveBytes DerivedPassword = new PasswordDeriveBytes(Password, SaltValueBytes, HashAlgorithm, PasswordIterations);
            byte[] KeyBytes = DerivedPassword.GetBytes(KeySize / 8);
            RijndaelManaged SymmetricKey = new RijndaelManaged();
            SymmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform Encryptor = SymmetricKey.CreateEncryptor(KeyBytes, InitialVectorBytes);
            MemoryStream MemStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(MemStream, Encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(PlainTextBytes, 0, PlainTextBytes.Length);
            cryptoStream.FlushFinalBlock();
            byte[] CipherTextBytes = MemStream.ToArray();
            MemStream.Close();
            cryptoStream.Close();
            return CipherTextBytes;
        }

        public static byte[] Decrypt(byte[] input, string Password, string Salt, string HashAlgorithm, int PasswordIterations, string InitialVector, int KeySize)
        {
            byte[] InitialVectorBytes = Convert.FromBase64String(InitialVector);
            byte[] SaltValueBytes = Convert.FromBase64String(Salt);
            byte[] CipherTextBytes = input;
            PasswordDeriveBytes DerivedPassword = new PasswordDeriveBytes(Password, SaltValueBytes, HashAlgorithm, PasswordIterations);
            byte[] KeyBytes = DerivedPassword.GetBytes(KeySize / 8);
            RijndaelManaged SymmetricKey = new RijndaelManaged();
            SymmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform Decryptor = SymmetricKey.CreateDecryptor(KeyBytes, InitialVectorBytes);
            MemoryStream MemStream = new MemoryStream(CipherTextBytes);
            CryptoStream cryptoStream = new CryptoStream(MemStream, Decryptor, CryptoStreamMode.Read);
            byte[] PlainTextBytes = new byte[CipherTextBytes.Length];
            int ByteCount = cryptoStream.Read(PlainTextBytes, 0, PlainTextBytes.Length);
            MemStream.Close();
            cryptoStream.Close();
            return PlainTextBytes;
        }

        public static string EncryptString(string PlainText, string Password, string Salt, string HashAlgorithm, int PasswordIterations, string InitialVector, int KeySize)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(PlainText), Password, Salt, HashAlgorithm, PasswordIterations, InitialVector, KeySize));
        }

        public static string DecryptString(string CipherText, string Password, string Salt, string HashAlgorithm, int PasswordIterations, string InitialVector, int KeySize)
        {
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(CipherText), Password, Salt, HashAlgorithm, PasswordIterations, InitialVector, KeySize));
        }
    }
}