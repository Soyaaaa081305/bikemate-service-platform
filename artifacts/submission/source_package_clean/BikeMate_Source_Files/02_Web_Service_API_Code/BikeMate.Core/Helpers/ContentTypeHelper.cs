namespace BikeMate.Core.Helpers;

public static class ContentTypeHelper
{
    public static string GuessFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".3gp" => "video/3gpp",
            _ => "application/octet-stream"
        };
    }
}
