namespace Sentinel.Plugin.SubFolder.Helpers
{
    public static class PathHelper
    {
        public static int GetRelativeDepth(string baseDir, string dir)
        {
            return Path.GetRelativePath(baseDir, dir).Count(c => c == Path.DirectorySeparatorChar);
        }
    }
}
