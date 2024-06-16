namespace ConsoleApp
{
    using System;

    public static class Extensions
    {
        public static string Clear(this string input)
        {
            return input.Trim().Replace(" ", "").Replace(Environment.NewLine, "");
        }
    }
}
