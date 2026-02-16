using System.Diagnostics;
using System.Text;

namespace CryptoSoft;

public class TransformationResult
{
    public bool Success { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class FileManager(string path)
{
    private string FilePath { get; } = path;

    // We process 1MB at a time. You can adjust this (4096 is common, 1024*1024 is faster for huge files)
    private const int BufferSize = 4096 * 4096;

    private bool CheckFile()
    {
        if (File.Exists(FilePath))
            return true;

        Console.WriteLine($"File not found: {FilePath}");
        return false;
    }

    public TransformationResult EncryptFile(string key, string outputFilePath)
    {
        // Guard clauses
        if (!CheckFile())
            return new TransformationResult { Success = false, ErrorMessage = "File not found" };

        if (string.IsNullOrEmpty(key))
            return new TransformationResult { Success = false, ErrorMessage = "Key is empty" };

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);

            using (var sourceStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            // Open (or create) the destination file for writing
            using (var destStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                var buffer = new byte[BufferSize];
                int bytesRead;
                long totalBytesProcessed = 0;

                // Loop through the file until no bytes remain
                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Process only the bytes we just read
                    ProcessChunk(buffer, bytesRead, keyBytes, totalBytesProcessed);

                    // Write immediately to disk
                    destStream.Write(buffer, 0, bytesRead);

                    // Track position for the XOR pattern to remain consistent across chunks
                    totalBytesProcessed += bytesRead;
                }
            }

            return new TransformationResult { Success = true, OutputPath = outputFilePath };
        }
        catch (Exception ex)
        {
            return new TransformationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Encrypts the buffer in place.
    /// </summary>
    private static void ProcessChunk(byte[] buffer, int bytesRead, byte[] keyBytes, long offset)
    {
        for (int i = 0; i < bytesRead; i++)
        {
            // We use (offset + i) to ensure the XOR pattern continues correctly 
            // from where the previous chunk left off.
            long globalIndex = offset + i;

            // Cast globalIndex to long/int carefully for the modulo
            buffer[i] = (byte)(buffer[i] ^ keyBytes[globalIndex % keyBytes.Length]);
        }
    }
}