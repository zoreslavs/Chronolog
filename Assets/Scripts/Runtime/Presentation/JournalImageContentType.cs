using System.IO;
using System;

namespace Chronolog.Presentation
{
    public static class JournalImageContentType
    {
        public static string GetForFilePath(string filePath)
        {
            var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

            if (fileExtension is ".jpg" or ".jpeg")
                return "image/jpeg";

            if (fileExtension == ".png")
                return "image/png";

            throw new ArgumentException("Only JPEG and PNG images are supported.", nameof(filePath));
        }
    }
}