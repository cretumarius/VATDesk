namespace VatDesk.Tests.Fixtures;

/// <summary>Proves test infrastructure + fixture path resolution work end to end.</summary>
public class SampleCsvFixtureTests
{
    [Fact]
    public void SampleCleanCsv_Has10DataRows()
    {
        var lines = File.ReadAllLines(SkillFixtures.Path("sample-clean.csv"));
        var dataRows = lines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));

        Assert.Equal(10, dataRows.Count());
    }
}
