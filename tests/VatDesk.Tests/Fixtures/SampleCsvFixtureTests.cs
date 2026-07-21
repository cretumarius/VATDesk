namespace VatDesk.Tests.Fixtures;

/// <summary>
/// Proves test infrastructure + fixture path resolution work end to end. No parsing logic yet —
/// that lands with CsvInvoiceParser in a later session.
/// </summary>
public class SampleCsvFixtureTests
{
    [Fact]
    public void SampleCleanCsv_Has10DataRows()
    {
        var path = RepoRelativePath(".claude/skills/hungarian-vat/assets/sample-clean.csv");

        var lines = File.ReadAllLines(path);
        var dataRows = lines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));

        Assert.Equal(10, dataRows.Count());
    }

    private static string RepoRelativePath(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "VatDesk.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException(
                "Could not locate repository root (VatDesk.sln) from test base directory.");
        }

        return Path.Combine(dir.FullName, relativePath);
    }
}
