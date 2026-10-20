using System;

namespace Deucarian.Simultria.API
{
    /// <summary>Content and filename for a file parameter in a generated multipart request.</summary>
    public sealed class SimultriaUpload
    {
        #region Constructors
        public SimultriaUpload(byte[] data, string fileName, string contentType = "application/octet-stream")
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("A filename is required.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentException("A content type is required.", nameof(contentType));
            }
            FileName = fileName;
            ContentType = contentType;
        }
        #endregion

        #region Properties
        public byte[] Data { get; }
        public string FileName { get; }
        public string ContentType { get; }
        #endregion
    }
}
