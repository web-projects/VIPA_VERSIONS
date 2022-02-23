using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TestHelper
{
    public static class RandomGenerator
    {
        public static Random rnd() => new Random((int)DateTime.Now.Ticks);

        public static int GetRandomValue() => rnd().Next();

        public static int GetRandomValue(int digits) => rnd().Next(1, (int)(Math.Pow(10.0, digits) - 1.0));
        
        public static string GetRandomValueStr(int digits)
        {
            StringBuilder sb = new StringBuilder();
            for (int ii = 0; ii < digits; ii++)
                sb.Append(rnd().Next(0, 9).ToString("G", new CultureInfo("en-us")));
            return sb.ToString();
        }
        public static string BuildRandomString(int string_length)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bit_count = (string_length * 6);
                var byte_count = ((bit_count + 7) / 8); // rounded up
                var bytes = new byte[byte_count];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes).TrimEnd('=');
            }
        }
    }
}
