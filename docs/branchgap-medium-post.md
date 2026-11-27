# Find the Branches Your Tests Never Take

*Why line coverage can look reassuring while important decisions in your code remain untested—and a small .NET tool that helps you find them.*

Your coverage report says 90%. The build is green. But somewhere in that codebase, an `if` statement has only ever taken one path.

That gap is easy to miss when you look at line coverage alone. A line can run during a test without every outcome on that line being exercised. Branch coverage gets closer to the question many developers actually want answered:

> Which decisions in my code have my tests never taken?

That is the problem **AStarDev.BranchGap**, or `branchgap`, is designed to make easier to investigate. It reads Cobertura coverage reports and ranks classes—or, when given your source code, methods—by how many branches remain untaken.

## Why look at branch coverage?

Consider:

```csharp
if (order.Total > creditLimit)
{
    RejectOrder(order);
}
```

A test could execute this line by using an order below the credit limit. The line is covered, but the rejection path is not. A branch-aware report distinguishes those outcomes and tells you there is still a path to test.

Branch coverage is not a quality score by itself. A high percentage cannot tell you whether your assertions are meaningful, and a low percentage does not automatically mean a bug. It is a useful lens: it can point you toward untested alternatives, boundary conditions, error paths, and other decisions worth reviewing.

## What `branchgap` does

`branchgap` takes Cobertura XML reports—such as the reports produced by .NET test runs—and turns their branch data into a prioritized list:

- **Ranks by the number of untaken branches**, so a class with many missed branches is not hidden just because its percentage looks moderate.
- **Aggregates nested and compiler-generated types** into the type that declares them, rather than flooding the report with compiler details.
- **Merges multiple reports**, taking the best covered count for each branch line found across reports.
- **Can map gaps back to methods and source constructs** when you provide the matching source tree.
- **Can emit table, Markdown, or JSON output**, and can enforce an overall branch-coverage threshold in CI.
- **Filters framework and common third-party namespaces** from the report so you can focus on the code your team owns. You can also add your own class-name glob exclusions.

## How to use it

### 1. Produce a Cobertura report

With the Microsoft Testing Platform, run:

```bash
dotnet test --coverage --coverage-output-format cobertura --results-directory TestResults
```

### 2. Install `branchgap`

The tool is not yet published to NuGet. For now, clone the repository and install it from a locally packed tool package:

```bash
dotnet pack src/AStarDev.BranchGap.Cli -c Release -o nupkg
dotnet tool install --global --add-source nupkg AStarDev.BranchGap
```

### 3. Find the biggest gaps

Point `branchgap` at one or more reports. The following searches for Cobertura reports under `TestResults`:

```bash
branchgap "TestResults/**/*.cobertura.xml"
```

By default, it shows the ten classes with the most untaken branches. Increase the number of results or merge reports from multiple test runs:

```bash
branchgap "TestResults/**/*.cobertura.xml" --top 25
branchgap unit.cobertura.xml integration.cobertura.xml
```

The default table separates untaken branches, branch coverage, and the covered/total count. For a machine-readable report or a Markdown table:

```bash
branchgap "TestResults/**/*.cobertura.xml" --format json
branchgap "TestResults/**/*.cobertura.xml" --format markdown
```

### 4. See the source behind a gap

Give the tool the source root that corresponds to the revision used to produce the report:

```bash
branchgap "TestResults/**/*.cobertura.xml" --source-root .
```

This adds the member, source line, and construct associated with each untaken branch. To rank methods rather than classes:

```bash
branchgap "TestResults/**/*.cobertura.xml" --source-root . --level method
```

The source files should match the report's revision so that line numbers and constructs line up. If the report was produced on another machine, `branchgap` attempts to resolve source paths by their trailing path components.

You can leave generated or out-of-scope classes out with repeatable class-name globs:

```bash
branchgap "TestResults/**/*.cobertura.xml" --exclude "*.Migrations.*" --exclude "MyCompany.Legacy.*"
```

Excluded classes are also removed from the overall coverage figure.

## Add a coverage gate to CI

`--fail-under` returns a failing exit code when the overall branch coverage is below the requested percentage. For example, a GitHub Actions workflow can add a Markdown report to its job summary:

```yaml
- run: dotnet test --coverage --coverage-output-format cobertura --results-directory TestResults
- run: branchgap "TestResults/**/*.cobertura.xml" --source-root . --format markdown --fail-under 80 >> "$GITHUB_STEP_SUMMARY"
```

Choose a threshold that fits your project and how quickly it is changing. The threshold is most useful as a visible, deliberate guardrail—not a substitute for reviewing whether tests exercise meaningful behavior.

## A few details about the numbers

The ranking prioritizes **the count of untaken branches**, then uses coverage percentage to break ties. That is why a class with 42 untaken branches can appear above one with 10, even if their percentages are in the same general range.

Cobertura reports the covered and total branches for a line, but not stable identifiers for each individual branch. When reports are merged, `branchgap` uses the best covered count reported for each branch line. Depending on the reports, that can understate the actual union of branches covered across runs. Treat merged figures as a practical summary, not a reconstruction of individual branch identities.

The tool focuses on decision points represented as branches in the Cobertura report. It does not decide which code is important, generate missing tests, or infer whether a test's assertions are strong. Its job is to make the next investigation easier: start with the biggest gaps, inspect the source, then decide which cases deserve tests.

## Under the hood

`branchgap` is a .NET command-line tool organized around a small pipeline:

1. A Cobertura provider reads class names, source files, and branch-line coverage.
2. The core merges reports, applies exclusions, and ranks classes by untaken branches.
3. If source is available, a Roslyn-based locator connects branch lines with their containing members and constructs.
4. The CLI formats the results as a table, Markdown, or JSON, and optionally checks the overall threshold.

This keeps the report tied to the coverage data you already generate while making the analysis useful both locally and in automation.

## Give it a try

If you already produce Cobertura reports, the quickest path is to run `branchgap` against them and inspect the first few results. A ranked list of untested decisions is often a more actionable starting point than a single coverage percentage.

**AStarDev.BranchGap is an open-source project.** Find the source, report issues, and follow its development at [astar-development/astardev-cli-tools on GitHub](https://github.com/astar-development/astardev-cli-tools).
