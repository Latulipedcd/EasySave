using System;
using System.IO;

namespace CryptoSoft
{
    /// <summary>
    /// Entry point for the CryptoSoft file transformation utility.
    /// </summary>
    internal class Program
    {
        // Standard exit codes
        private const int ExitSuccess = 0;
        private const int ExitUsageError = 1;
        private const int ExitOperationError = 2;

        /// <summary>
        /// Main entry point for the application.
        /// Reads the source file, XOR-encrypts it in chunks, and streams the encrypted
        /// bytes to stdout. The caller is responsible for writing those bytes to disk.
        /// All diagnostic messages go to stderr so stdout remains pure binary data.
        /// </summary>
        /// <param name="args">Command line arguments: source-file-path, key</param>
        static void Main(string[] args)
        {
            try
            {
                if (args is null || args.Length < 2)
                {
                    Console.Error.WriteLine("Usage: CryptoSoft <source-file-path> <key>");
                    Console.Error.WriteLine("Arguments:");
                    Console.Error.WriteLine("  source-file-path : Path to the file to transform");
                    Console.Error.WriteLine("  key              : Encryption/decryption key");
                    Console.Error.WriteLine("Output:");
                    Console.Error.WriteLine("  Encrypted bytes are written to stdout.");
                    Environment.ExitCode = ExitUsageError;
                    return;
                }

                string sourceFilePath = args[0];
                string key = args[1];

                var fileManager = new FileManager(sourceFilePath);

                Console.Error.WriteLine($"Processing file: {sourceFilePath}...");

                // Write encrypted bytes directly to the raw stdout stream (binary, no encoding)
                using var stdout = Console.OpenStandardOutput();
                var result = fileManager.TransformFile(key, stdout);

                if (!result.Success)
                {
                    Console.Error.WriteLine($"Transformation failed: {result.ErrorMessage}");
                    Environment.ExitCode = ExitOperationError;
                    return;
                }

                Console.Error.WriteLine("Transformation complete.");
                Environment.ExitCode = ExitSuccess;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Critical Error: {e.Message}");
                Environment.ExitCode = ExitOperationError;
            }
        }
    }
}