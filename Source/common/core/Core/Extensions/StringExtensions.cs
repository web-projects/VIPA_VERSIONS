namespace Core.Extensions
{
    public static class StringExtensions
    {
        public static string Left(this string input, int maxLength)
            => input is null ? null : input.Length > maxLength ? input.Substring(0, maxLength) : input;
    }
}
