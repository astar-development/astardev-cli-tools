using Shouldly;
using TestGap.Core.Abstractions;

namespace TestGap.Coverage.Cobertura.TestsUnit;

public class ProgramShould
{
    private ICoverageProvider provider;

    public ProgramShould() => provider = new CoberturaCoverageProvider();

    [Fact]
    public async Task loadAsync_and_return_empty_coverage_when_there_are_no_classes()
    {
        var result = await provider.LoadAsync(TestFile.GetPath("EmptyCoverage.xml"), TestContext.Current.CancellationToken);

        result.Classes.Count.ShouldBe(0);
    }

    [Fact]
    public async Task loadAsync_reads_class_and_detects_name_and_namespace_correctly()
    {
        var analysis = await provider.LoadAsync(TestFile.GetPath("SingleClass.xml"), TestContext.Current.CancellationToken);

        analysis.Classes.Count.ShouldBe(1);

        analysis.Classes.Single().FullyQualifiedName.ShouldBe("Foo.Bar.SyncEngine");
    }
}
