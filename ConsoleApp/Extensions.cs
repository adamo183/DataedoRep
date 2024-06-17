namespace ConsoleApp
{
    using System;

    public static class Extensions
    {
        public static string Clear(this string input)
        {
            if (input == null)
                return string.Empty;
            return input.Trim().Replace(" ", "").Replace(Environment.NewLine, "");
        }
    }
}
