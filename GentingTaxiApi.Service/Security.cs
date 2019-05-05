using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Crypto.Paddings;

namespace GentingTaxiApi.Service
{
    public class Security
    {
        public static string getSalt()
        {
            var saltBytes = new byte[32];
            using (var provider = new RNGCryptoServiceProvider()) provider.GetNonZeroBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }
        public static string getHash(string salt, string password)
        {
            var saltBytes = Convert.FromBase64String(salt);
            using (var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, saltBytes, 1000))
                return Convert.ToBase64String(rfc2898DeriveBytes.GetBytes(256));
        }
        private const string _chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890~!@#$%^&*()-+:,./?";
        private static readonly Random _rng = new Random();
        public static string RandomString(int size)
        {
            char[] buffer = new char[size];

            for (int i = 0; i < size; i++)
            {
                buffer[i] = _chars[_rng.Next(_chars.Length)];
            }
            return new string(buffer);
        }
        private const string _chars2 = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        private static readonly Random _rng2 = new Random();
        public static string RandomString2(int size)
        {
            char[] buffer = new char[size];

            for (int i = 0; i < size; i++)
            {
                buffer[i] = _chars2[_rng2.Next(_chars2.Length)];
            }
            return new string(buffer);
        }
        //public static string checkHMAC(string key, string message)
        //{
        //    string result = "";
        //    if (key == null) return result;
        //    if (message == null) return result;


        //    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

        //    byte[] keyByte = encoding.GetBytes(key);

        //    HMACSHA256 hmacsha256 = new HMACSHA256(keyByte);

        //    byte[] messageBytes = encoding.GetBytes(message);

        //    byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
        //    result = Convert.ToBase64String(hashmessage);
        //    return result;
        //}
        public static string checkHMAC(string key, string message)
        {
            string result = "";
            if (key == null) return result;
            if (message == null) return result;


            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

            byte[] keyByte = encoding.GetBytes(key);

            HMACSHA256 hmacsha256 = new HMACSHA256(keyByte);

            byte[] messageBytes = encoding.GetBytes(message);

            byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
            result = ByteToString2(hashmessage);
            return result;
        }
        //public static string encodeHMAC(string key, string message)
        //{

        //    string result = "";
        //    if (key == null) return result;
        //    if (message == null) return result;
        //    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

        //    byte[] keyByte = encoding.GetBytes(key);

        //    HMACSHA256 hmacsha256 = new HMACSHA256(keyByte);

        //    byte[] messageBytes = encoding.GetBytes(message);

        //    byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
        //    result = ByteToString(hashmessage);
        //    return result;
        //}
        //public static string decodeHMAC(string key, string message)
        //{

        //    string result = "";
        //    if (key == null) return result;
        //    if (message == null) return result;

        //    byte[] keyByte = UTF8Encoding.UTF8.GetBytes(key);

        //    HMACSHA256 hmacsha256 = new HMACSHA256(keyByte);

        //    byte[] messageBytes = StringToByte(message);

        //    byte[] hashmessage1 = hmacsha256.ComputeHash(messageBytes);
        //    byte[] hashmessage = hmacsha256.TransformFinalBlock(hashmessage1, 0, hashmessage1.Length);

        //    result = UTF8Encoding.UTF8.GetString(hashmessage);
        //    string a = Convert.ToBase64String(hashmessage);
        //    string b = ByteToString(hashmessage);
        //    return result;
        //}
        public static string Encrypt(string key, string toEncrypt)
        {
            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            // Get the key from config file
            //System.Windows.Forms.MessageBox.Show(key);
            //If hashing use get hashcode regards to your key
            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            //Always release the resources and flush data
            // of the Cryptographic service provide. Best Practice

            hashmd5.Clear();


            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            //set the secret key for the tripleDES algorithm
            tdes.Key = keyArray;
            //mode of operation. there are other 4 modes.
            //We choose ECB(Electronic code Book)
            tdes.Mode = CipherMode.ECB;
            //padding mode(if any extra byte added)

            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            //transform the specified region of bytes array to resultArray
            byte[] resultArray =
              cTransform.TransformFinalBlock(toEncryptArray, 0,
              toEncryptArray.Length);
            //Release resources held by TripleDes Encryptor
            tdes.Clear();
            //Return the encrypted data into unreadable string format
            return ByteToString(resultArray);//Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }
        public static string Decrypt(string key, string cipherString)
        {
            byte[] keyArray;
            //get the byte code of the string

            byte[] toEncryptArray = StringToByte(cipherString);//Convert.FromBase64String(cipherString);

            //if hashing was used get the hash code with regards to your key
            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            //release any resource held by the MD5CryptoServiceProvider

            hashmd5.Clear();

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            //set the secret key for the tripleDES algorithm
            tdes.Key = keyArray;
            //mode of operation. there are other 4 modes. 
            //We choose ECB(Electronic code Book)

            tdes.Mode = CipherMode.ECB;
            //padding mode(if any extra byte added)
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(
                                 toEncryptArray, 0, toEncryptArray.Length);
            //Release resources held by TripleDes Encryptor                
            tdes.Clear();
            //return the Clear decrypted TEXT
            return UTF8Encoding.UTF8.GetString(resultArray);
        }

        public static string Md5(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public static string ByteToString(byte[] buff)
        {
            string sbinary = "";

            for (int i = 0; i < buff.Length; i++)
            {
                sbinary += buff[i].ToString("X2"); // hex format
            }
            return (sbinary);
        }
        public static string ByteToString2(byte[] buff)
        {
            string sbinary = "";

            for (int i = 0; i < buff.Length; i++)
            {
                sbinary += buff[i].ToString("x2"); // hex format
            }
            return (sbinary);
        }
        public static byte[] StringToByte(string str)
        {
            Dictionary<string, byte> hexindex = new Dictionary<string, byte>();
            for (byte i = 0; i < 255; i++)
                hexindex.Add(i.ToString("X2"), i);

            List<byte> hexres = new List<byte>();
            for (int i = 0; i < str.Length; i += 2)
                hexres.Add(hexindex[str.Substring(i, 2)]);

            return hexres.ToArray();
        }
    }
}
