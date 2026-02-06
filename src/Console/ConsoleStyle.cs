namespace EasySave.ConsoleApp
{
    internal static class ConsoleStyle
    {
        public static void ClearAndWriteHeader(string title)
        {
            Console.Clear();

            int width = Math.Max(34, title.Length + 8);
            string border = new string('=', width);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(border);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(CenterText(title, width));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(border);
            Console.ResetColor();
        }

        public static void WriteMenuLine(string text)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void WritePrompt(string text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(text);
            Console.ResetColor();
        }

        public static void WriteInfo(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void WriteError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void Pause(bool isFrench)
        {
            Console.WriteLine();
            WriteInfo(isFrench
                ? "Press Enter / Entree pour continuer..."
                : "Press Enter to continue...");
            Console.ReadLine();
        }

        public static void Reset()
        {
            Console.ResetColor();
        }

        private static string CenterText(string text, int width)
        {
            if (text.Length >= width)
            {
                return text;
            }

            int leftPadding = (width - text.Length) / 2;
            return new string(' ', leftPadding) + text;
        }
    }
}
