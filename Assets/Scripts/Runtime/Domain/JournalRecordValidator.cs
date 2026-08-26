namespace Chronolog.Domain
{
    public static class JournalRecordValidator
    {
        public static bool TryValidate(string content, string localImagePath, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                errorMessage = "Content is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(localImagePath))
            {
                errorMessage = "An image is required.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}