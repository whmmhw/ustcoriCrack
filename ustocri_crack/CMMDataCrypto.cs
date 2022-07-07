// Decompiled with JetBrains decompiler
// Type: USTCORi.WebLabClient.Common.CMMDataCrypto
// Assembly: WebLabClient, Version=1.1.7909.28014, Culture=neutral, PublicKeyToken=null
// MVID: 7C459EFC-E027-430B-B442-D8950A7658E9
// Assembly location: E:\vm\files\WebLabClient.exe

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using USTCORI.CommonLib.Security;

namespace ustcori_crack
{
  public class CMMDataCrypto
  {
    private static string Key;
    private static readonly byte[] IvBytes = new byte[8]
    {
      (byte) 1,
      (byte) 35,
      (byte) 69,
      (byte) 103,
      (byte) 137,
      (byte) 171,
      (byte) 205,
      (byte) 239
    };

    public CMMDataCrypto() => CMMDataCrypto.Key = "USTCORIX";

    public string Encrypt(string pToEncrypt)
    {
      DESCryptoServiceProvider cryptoServiceProvider = new DESCryptoServiceProvider();
      byte[] bytes = Encoding.UTF8.GetBytes(pToEncrypt);
      cryptoServiceProvider.Key = Encoding.ASCII.GetBytes(CMMDataCrypto.Key);
      cryptoServiceProvider.IV = Encoding.ASCII.GetBytes(CMMDataCrypto.Key);
      MemoryStream memoryStream = new MemoryStream();
      CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, cryptoServiceProvider.CreateEncryptor(), CryptoStreamMode.Write);
      cryptoStream.Write(bytes, 0, bytes.Length);
      cryptoStream.FlushFinalBlock();
      StringBuilder stringBuilder = new StringBuilder();
      foreach (byte num in memoryStream.ToArray())
        stringBuilder.AppendFormat("{0:X2}", (object) num);
      return stringBuilder.ToString();
    }

    public string Decrypt(string pToDecrypt)
    {
      DESCryptoServiceProvider cryptoServiceProvider = new DESCryptoServiceProvider();
      byte[] buffer = new byte[pToDecrypt.Length / 2];
      for (int index = 0; index < pToDecrypt.Length / 2; ++index)
      {
        int int32 = Convert.ToInt32(pToDecrypt.Substring(index * 2, 2), 16);
        buffer[index] = (byte) int32;
      }
      cryptoServiceProvider.Key = Encoding.ASCII.GetBytes(CMMDataCrypto.Key);
      cryptoServiceProvider.IV = Encoding.ASCII.GetBytes(CMMDataCrypto.Key);
      MemoryStream memoryStream = new MemoryStream();
      CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, cryptoServiceProvider.CreateDecryptor(), CryptoStreamMode.Write);
      cryptoStream.Write(buffer, 0, buffer.Length);
      cryptoStream.FlushFinalBlock();
      StringBuilder stringBuilder = new StringBuilder();
      return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    public string DecryptAES(string decryptString, string decryptKey, string salt)
    {
      AesManaged aesManaged = (AesManaged) null;
      MemoryStream memoryStream = (MemoryStream) null;
      CryptoStream cryptoStream = (CryptoStream) null;
      try
      {
        Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(decryptKey, Encoding.UTF8.GetBytes(salt));
        aesManaged = new AesManaged();
        aesManaged.Key = rfc2898DeriveBytes.GetBytes(aesManaged.KeySize / 8);
        aesManaged.IV = rfc2898DeriveBytes.GetBytes(aesManaged.BlockSize / 8);
        memoryStream = new MemoryStream();
        cryptoStream = new CryptoStream((Stream) memoryStream, aesManaged.CreateDecryptor(), CryptoStreamMode.Write);
        byte[] buffer = Convert.FromBase64String(decryptString);
        cryptoStream.Write(buffer, 0, buffer.Length);
        cryptoStream.FlushFinalBlock();
        return Encoding.UTF8.GetString(memoryStream.ToArray(), 0, memoryStream.ToArray().Length);
      }
      catch
      {
        return decryptString;
      }
      finally
      {
        cryptoStream?.Close();
        memoryStream?.Close();
        aesManaged?.Clear();
      }
    }

    public string EncryptAES(string encryptString, string encryptKey, string salt)
    {
      AesManaged aesManaged = (AesManaged) null;
      MemoryStream memoryStream = (MemoryStream) null;
      CryptoStream cryptoStream = (CryptoStream) null;
      try
      {
        Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(encryptKey, Encoding.UTF8.GetBytes(salt));
        aesManaged = new AesManaged();
        aesManaged.Key = rfc2898DeriveBytes.GetBytes(aesManaged.KeySize / 8);
        aesManaged.IV = rfc2898DeriveBytes.GetBytes(aesManaged.BlockSize / 8);
        memoryStream = new MemoryStream();
        cryptoStream = new CryptoStream((Stream) memoryStream, aesManaged.CreateEncryptor(), CryptoStreamMode.Write);
        byte[] bytes = Encoding.UTF8.GetBytes(encryptString);
        cryptoStream.Write(bytes, 0, bytes.Length);
        cryptoStream.FlushFinalBlock();
        return Convert.ToBase64String(memoryStream.ToArray());
      }
      catch
      {
        return encryptString;
      }
      finally
      {
        cryptoStream?.Close();
        memoryStream?.Close();
        aesManaged?.Clear();
      }
    }

    public string EncryptA(string pToEncrypt) => this.EncryptAES(pToEncrypt, CMMDataCrypto.Key, CMMDataCrypto.Key);

    public string DecryptA(string pToDecrypt) => this.DecryptAES(pToDecrypt, CMMDataCrypto.Key, CMMDataCrypto.Key);

    public string MD5JiaMi(string password) => MD5Encoder.GetMd5Hash(password);

    public static string DESEncrypt(string input, string key)
    {
      try
      {
        byte[] bytes1 = Encoding.UTF8.GetBytes(key);
        DES des = DES.Create();
        des.Mode = CipherMode.ECB;
        des.Padding = PaddingMode.Zeros;
        using (MemoryStream memoryStream = new MemoryStream())
        {
          byte[] bytes2 = Encoding.UTF8.GetBytes(input);
          using (CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, des.CreateEncryptor(bytes1, CMMDataCrypto.IvBytes), CryptoStreamMode.Write))
          {
            cryptoStream.Write(bytes2, 0, bytes2.Length);
            cryptoStream.FlushFinalBlock();
          }
          return Convert.ToBase64String(memoryStream.ToArray());
        }
      }
      catch
      {
        return input;
      }
    }

    public static string DESDecrypt(string input, string key)
    {
      try
      {
        byte[] bytes = Encoding.UTF8.GetBytes(key);
        DES des = DES.Create();
        des.Mode = CipherMode.ECB;
        des.Padding = PaddingMode.Zeros;
        using (MemoryStream memoryStream = new MemoryStream())
        {
          byte[] buffer = Convert.FromBase64String(input);
          using (CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, des.CreateDecryptor(bytes, CMMDataCrypto.IvBytes), CryptoStreamMode.Write))
          {
            cryptoStream.Write(buffer, 0, buffer.Length);
            cryptoStream.FlushFinalBlock();
          }
          return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
      }
      catch
      {
        return input;
      }
    }

    public static string AesEncrypt(byte[] toEncryptArray, string key)
    {
      byte[] numArray1 = Convert.FromBase64String(key);
      byte[] numArray2 = new byte[16];
      for (int index = 0; index < 16; ++index)
        numArray2[index] = numArray1[index];
      Rijndael rijndael = Rijndael.Create();
      rijndael.Key = numArray1;
      rijndael.IV = numArray2;
      rijndael.Mode = CipherMode.CBC;
      rijndael.Padding = PaddingMode.None;
      byte[] inArray = rijndael.CreateEncryptor().TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
      return Convert.ToBase64String(inArray, 0, inArray.Length);
    }

    public static byte[] AesDecrypt(string str, string key)
    {
      byte[] numArray1 = Convert.FromBase64String(key);
      byte[] buffer = Convert.FromBase64String(str);
      byte[] numArray2 = new byte[16];
      for (int index = 0; index < 16; ++index)
        numArray2[index] = numArray1[index];
      SymmetricAlgorithm symmetricAlgorithm = (SymmetricAlgorithm) Rijndael.Create();
      symmetricAlgorithm.Key = numArray1;
      symmetricAlgorithm.IV = numArray2;
      symmetricAlgorithm.Mode = CipherMode.CBC;
      symmetricAlgorithm.Padding = PaddingMode.None;
      MemoryStream memoryStream = new MemoryStream(buffer);
      CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, symmetricAlgorithm.CreateDecryptor(), CryptoStreamMode.Read);
      cryptoStream.Read(buffer, 0, buffer.Length);
      cryptoStream.Close();
      memoryStream.Close();
      return buffer;
    }
  }
}
