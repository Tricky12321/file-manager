using System.IO;

namespace FileManager;

public static class Extensions
{
    public static long GetFileSize(this string path)
    {
        return new FileInfo(path).Length;
    }
    
    public static bool IsNullOrWhitespace(this string path)
    {
        return string.IsNullOrWhiteSpace(path);
    }
}
