using System.Buffers;
using System.Text;

namespace CryptoSoft
{
    /// <summary>
    /// Represents the result of a file transformation operation.
    /// </summary>
    internal class FileOperationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Manages file transformation operations using XOR cipher.
    /// Note: XOR cipher is symmetric - the same operation encrypts and decrypts.
    /// </summary>
    /// <param name="path">The path to the source file to process.</param>
    internal class FileManager(string path)
    {
        private string FilePath { get; } = path;

        // We process 1MB at a time. This can be adjusted (4096 is common, 1024*1024 is faster for huge files)
        private const int BufferSize = 1024 * 1024;

        /// <summary>
        /// Transforms a file using XOR cipher with the provided key, writing the
        /// encrypted bytes to <paramref name="outputStream"/>.
        /// The caller is responsible for opening and closing the stream.
        /// </summary>
        /// <param name="key">The encryption key as a string.</param>
        /// <param name="outputStream">The stream that receives the encrypted bytes.</param>
        internal FileOperationResult TransformFile(string key, Stream outputStream)
        {
            if (!File.Exists(FilePath))
                return new FileOperationResult { Success = false, ErrorMessage = $"Source file not found: {FilePath}" };

            if (string.IsNullOrEmpty(key))
                return new FileOperationResult { Success = false, ErrorMessage = "Key cannot be empty" };

            // Rent buffer from pool to reduce GC pressure
            byte[]? buffer = null;
            try
            {
                var keyBytes = Encoding.UTF8.GetBytes(key);
                buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

                using var sourceStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);

                int bytesRead;
                long totalBytesProcessed = 0;

                // Loop through the file in chunks, XOR-encrypt each chunk, stream to output
                while ((bytesRead = sourceStream.Read(buffer, 0, BufferSize)) > 0)
                {
                    ApplyXorCipher(buffer.AsSpan(0, bytesRead), keyBytes, totalBytesProcessed);
                    outputStream.Write(buffer, 0, bytesRead);
                    totalBytesProcessed += bytesRead;
                }

                outputStream.Flush();
                return new FileOperationResult { Success = true };
            }
            catch (UnauthorizedAccessException ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    ErrorMessage = $"Access denied: {ex.Message}"
                };
            }
            catch (IOException ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    ErrorMessage = $"I/O error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                };
            }
            finally
            {
                // Always return the rented buffer to the pool
                if (buffer != null)
                    ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Applies XOR cipher transformation to a buffer in place.
        /// The transformation uses a repeating key pattern that continues consistently across multiple chunks.
        /// </summary>
        /// <param name="buffer">The buffer to transform.</param>
        /// <param name="keyBytes">The key bytes to use for XOR operations.</param>
        /// <param name="offset">The global byte offset for this chunk to maintain consistent key pattern.</param>
        private static void ApplyXorCipher(Span<byte> buffer, byte[] keyBytes, long offset)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                // We use (offset + i) to ensure the XOR pattern continues correctly 
                // from where the previous chunk left off.
                long globalIndex = offset + i;

                buffer[i] ^= keyBytes[globalIndex % keyBytes.Length];
            }
        }
    }
}