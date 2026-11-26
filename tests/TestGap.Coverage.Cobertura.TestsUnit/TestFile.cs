namespace TestGap.Coverage.Cobertura.TestsUnit;

public static class TestFile
{
    public static string GetPath(string fileName)
        => Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            fileName);
}