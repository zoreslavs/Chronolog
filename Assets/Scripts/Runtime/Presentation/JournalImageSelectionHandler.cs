using Chronolog.Persistence;
using Chronolog.Domain;
using System;

namespace Chronolog.Presentation
{
    public sealed class JournalImageSelectionHandler
    {
        private readonly Action<string, JournalImageSource> imageSelected;
        private readonly JournalImageStorage imageStorage;

        public JournalImageSelectionHandler(JournalImageStorage imageStorage, Action<string, JournalImageSource> imageSelected)
        {
            this.imageStorage = imageStorage ?? throw new ArgumentNullException(nameof(imageStorage));
            this.imageSelected = imageSelected ?? throw new ArgumentNullException(nameof(imageSelected));
        }

        public void Complete(string sourceImagePath, JournalImageSource imageSource)
        {
            if (string.IsNullOrEmpty(sourceImagePath))
                return;

            var localImagePath = imageStorage.CopyToLocalStorage(sourceImagePath);
            imageSelected(localImagePath, imageSource);
        }
    }
}