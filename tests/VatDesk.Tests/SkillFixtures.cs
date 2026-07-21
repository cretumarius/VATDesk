namespace VatDesk.Tests;

/// <summary>Resolves paths into .claude/skills/hungarian-vat/assets/ regardless of test runner CWD.</summary>
internal static class SkillFixtures
{
    public static string Path(string relativeAssetPath) =>
        System.IO.Path.Combine(RepoRoot(), ".claude/skills/hungarian-vat/assets", relativeAssetPath);

    public static Stream OpenRead(string relativeAssetPath) => File.OpenRead(Path(relativeAssetPath));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(System.IO.Path.Combine(dir.FullName, "VatDesk.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Could not locate repository root (VatDesk.sln) from test base directory.");
        }

        return dir.FullName;
    }
}
