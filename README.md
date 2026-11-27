# AStarDev.BranchGap

Line and method coverage is easy to game; **untaken branches** are not. `branchgap` reads Cobertura coverage reports and lists the classes (or members) with the most branches your tests never take, and which `if`, `switch`, `??`... each one is.

## Install

Not published to NuGet yet. From a clone:

```bash
dotnet pack src/AStarDev.BranchGap.Cli -c Release -o nupkg
dotnet tool install --global --add-source nupkg AStarDev.BranchGap
```

## Use

Produce Cobertura with the Microsoft Testing Platform:

```bash
dotnet test --coverage --coverage-output-format cobertura --results-directory TestResults
```

```bash
branchgap "TestResults/**/*.cobertura.xml"                      # worst 10 classes
branchgap a.cobertura.xml b.cobertura.xml -n 25                  # reports are merged
branchgap "TestResults/**/*.cobertura.xml" --source-root .       # + which construct is untaken
branchgap "TestResults/**/*.cobertura.xml" --source-root . --level method
branchgap "TestResults/**/*.cobertura.xml" --fail-under 85       # CI gate
```

| Option | Meaning |
|---|---|
| `<reports>...` | Cobertura XML files. `*`/`?` in the file name are expanded (also on Windows); `**` in the folder part searches subfolders. Reports are merged. |
| `-n, --top <n>` | How many rows to show (default 10). |
| `-x, --exclude <glob>` | Class-name glob to leave out, e.g. `*.Migrations.*`. Repeatable. Also removes it from the overall figure. |
| `-f, --format table\|json\|markdown` | Output format (default `table`). |
| `--fail-under <pct>` | Exit code `1` when overall branch coverage is below `pct`. |
| `--source-root <dir>` | Folder with the sources named in the report. Paths from another machine are matched by their tail. Adds the member, construct and code line of each untaken branch (Roslyn). |
| `--level class\|method` | Rank classes (default) or members; `method` needs `--source-root`. |

Exit codes: `0` ok, `1` below `--fail-under`, `2` bad input (missing report/source root, invalid XML), other non-zero for argument errors.

### CI example

```yaml
- run: dotnet test --coverage --coverage-output-format cobertura --results-directory TestResults
- run: branchgap "TestResults/**/*.cobertura.xml" --source-root . --format markdown --fail-under 80 >> "$GITHUB_STEP_SUMMARY"
```

## How it ranks

Classes are ranked by **number of untaken branches** (a 0/2 class should not outrank a 200/400 one), lowest percentage breaking ties. Classes with no branches and compiler-generated types are dropped; nested and generated types (`Foo/<Bar>d__1`, `Foo.<>c`) count towards the type that declares them.

When reports are merged, a branch line takes the best covered count seen in any report. Cobertura only says "n of m" per line, not *which* branch, so the merged figure can under-report the true union.

## Cobertura fields used

`package/@name` (assembly), `class/@name` and `@filename`, and for each `line` with `branch="True"`: `@number` and `@condition-coverage` (`"50% (1/2)"` = covered/total). Lines repeated under both a method and the class are counted once. Sample reports live in `tests/AStarDev.BranchGap.Coverage.Cobertura.TestsUnit/TestData`.

## Layout

| Project | Role |
|---|---|
| `AStarDev.BranchGap.Core` | Models, merge, ranking, use case, abstractions |
| `AStarDev.BranchGap.Coverage.Cobertura` | Cobertura provider |
| `AStarDev.BranchGap.Roslyn` | Maps branch lines to members/constructs |
| `AStarDev.BranchGap.Cli` | The `branchgap` tool |
