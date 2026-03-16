namespace TemizPC.Core.Utilities;

public static class PathSafety
{
    public static bool IsWithinAllowedRoots(string candidatePath, IEnumerable<string> allowedRoots)
    {
        return allowedRoots.Any(root => IsUnderRoot(candidatePath, root));
    }

    public static bool IsUnderRoot(string candidatePath, string allowedRoot)
    {
        if (string.IsNullOrWhiteSpace(candidatePath) || string.IsNullOrWhiteSpace(allowedRoot))
        {
            return false;
        }

        var candidate = Normalize(candidatePath);
        var root = Normalize(allowedRoot);

        return candidate.Equals(root, StringComparison.OrdinalIgnoreCase)
            || candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || candidate.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string path)
    {
        return Path.GetFullPath(path.Trim())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
