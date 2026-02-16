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
        /// </summary>
        /// <param name="args">Command line arguments: source-file-path, target-file-path, key</param>
        static void Main(string[] args)
        {
            try
            {
                if (args is null || args.Length < 3)
                {
                    Console.WriteLine("Usage: CryptoSoft <source-file-path> <target-file-path> <key>");
                    Console.WriteLine("Arguments:");
                    Console.WriteLine("  source-file-path : Path to the file to transform");
                    Console.WriteLine("  target-file-path : Path where the output will be written");
                    Console.WriteLine("  key              : Encryption/decryption key");
                    Environment.ExitCode = ExitUsageError;
                    return;
                }

                string sourceFilePath = args[0];
                string targetFilePath = args[1];
                string key = args[2];

                var fileManager = new FileManager(sourceFilePath);

                Console.WriteLine($"Processing file: {sourceFilePath}...");

                var result = fileManager.TransformFile(key, targetFilePath);

                if (!result.Success)
                {
                    Console.WriteLine($"Transformation failed: {result.ErrorMessage}");

                    // Cleanup: Delete partial file if it was created during a failed attempt
                    if (File.Exists(targetFilePath)) 
                    {
                        File.Delete(targetFilePath);
                        Console.WriteLine("Partial output file has been cleaned up.");
                    }

                    Environment.ExitCode = ExitOperationError;
                    return;
                }

                Console.WriteLine($"Success! Transformed file written to: {targetFilePath}");
                Environment.ExitCode = ExitSuccess;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Critical Error: {e.Message}");
                Environment.ExitCode = ExitOperationError;
            }
        }
    }
}