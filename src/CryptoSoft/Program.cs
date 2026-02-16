using CryptoSoft;

internal static class Program
{
    private static void Main(string[] args)
    {
        try
        {

            if (args is null || args.Length < 3)
            {
                Console.WriteLine("Usage: CryptoSoft <source-file-path> <target-file-path> <key>");
                Environment.Exit(-1);
            }

            string sourcefilePath = args[0];
            string targetfilePath = args[1];
            string key = args[2];



            var fileManager = new FileManager(sourcefilePath);


            Console.WriteLine($"Processing file: {sourcefilePath}...");


            var result = fileManager.EncryptFile(key, targetfilePath);


            if (!result.Success)
            {
                Console.WriteLine($"Encryption failed: {result.ErrorMessage}");

                // Cleanup: Delete partial file if it was created during a failed attempt
                if (File.Exists(targetfilePath)) File.Delete(targetfilePath);

                Environment.Exit(-1);
            }

            Console.WriteLine($"Success! Encrypted file written to: {targetfilePath}");
            Environment.Exit(0);

        }
        catch (Exception e)
        {
            Console.WriteLine($"Critical Error: {e.Message}");
            Environment.Exit(-99);
        }
    }
}