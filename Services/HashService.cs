using System;
using System.IO;
using System.Security.Cryptography;

namespace SVMKurs.Services
{
    public static class HashService
    {
        /// <summary>
        /// Вычисляет MD5 хэш файла
        /// </summary>
        public static string ComputeFileHash(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Вычисляет MD5 хэш из байтов
        /// </summary>
        public static string ComputeHash(byte[] data)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}