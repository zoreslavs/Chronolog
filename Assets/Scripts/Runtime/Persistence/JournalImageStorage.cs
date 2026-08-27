using System.IO;
using System;

namespace Chronolog.Persistence
{
    public sealed class JournalImageStorage
    {
        private const string ImagesDirectoryName = "images";
        private readonly string storageDirectoryPath;

        public JournalImageStorage(string storageDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(storageDirectoryPath))
                throw new ArgumentException("Storage directory path is required.", nameof(storageDirectoryPath));

            this.storageDirectoryPath = storageDirectoryPath;
        }

        public string CopyToLocalStorage(string sourceImagePath)
        {
            if (string.IsNullOrWhiteSpace(sourceImagePath))
                throw new ArgumentException("Source image path is required.", nameof(sourceImagePath));

            var imagesDirectoryPath = Path.Combine(storageDirectoryPath, ImagesDirectoryName);
            Directory.CreateDirectory(imagesDirectoryPath);

            var destinationImagePath = Path.Combine(imagesDirectoryPath, $"{Guid.NewGuid():N}{Path.GetExtension(sourceImagePath)}");
            File.Copy(sourceImagePath, destinationImagePath);

            return destinationImagePath;
        }
    }
}