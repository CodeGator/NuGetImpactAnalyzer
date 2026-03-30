# NuGet Impact Analyzer

**NuGet Impact Analyzer** is a Windows desktop application for understanding how changes to a .NET project or NuGet package ripple across **multiple Git repositories** at once. It is aimed at organizations that split libraries across many repos (for example, a family of packages that reference each other) and need to see **who depends on what** before upgrading or republishing a package.

The app is built with **WPF** on **.NET 10** (`net10.0-windows`).

## What it does

1. **Workspace**: You define a set of Git repositories (URLs, branches, optional analysis subfolder) in configuration.
2. **Sync**: The tool clones or updates those repositories locally (with optional credentials for private hosts).
3. **Dependency graph**: It scans every `*.csproj` in the cloned trees, resolves **project-to-project** references and **package** references, and builds a single graph that spans all configured repos.
4. **Impact analysis**: You enter a package or project name. The tool finds matching nodes and lists **all transitive dependents**—everything that ultimately depends on that target through the graph.
5. **Build order**: From a chosen target, it can suggest an order in which to build projects (dependencies before dependents).

Results distinguish **definite** vs **possible** impact: along paths where a dependency is modeled as a package edge, the tool checks whether the referenced version range is **satisfied** by the dependent’s resolved version. Project-reference edges are always treated as definite. Unsatisfied or ambiguous package constraints contribute to “possible” impact.

Central package management is respected: `Directory.Packages.props` files on the path from the repo root to each project are merged so package versions align with your repo layout.

## How it works (technical overview)

| Step | Behavior |
|------|----------|
| **Configuration** | App settings and repo list are loaded from JSON. A sample list ships with the project at `src/NuGetImpactAnalyzer/config.json`. Mutable data (clones, preferences, credential metadata) lives under `%LocalAppData%\NuGetImpactAnalyzer` (repositories under `...\repos`). |
| **Graph construction** | For each repo, all `.csproj` files are parsed for `ProjectReference` and `PackageReference`. Edges to other projects in the same or another repo are created when paths or package identities match known projects in the workspace. Nodes are identified as `RepoName/ProjectName` for stable display and analysis. |
| **Impact query** | Starting from nodes whose name matches your query, the tool walks the graph **backward** (dependents only) and collects the transitive closure. Each dependent is labeled using path analysis over version constraints where applicable. |
| **UI** | The main window hosts repository management, sync, graph summary, impact search, and a shared log. Master password and per-repository credential flows support private Git remotes. |

## Requirements

- **OS**: Windows (WPF).
- **SDK**: [.NET 10 SDK](https://dotnet.microsoft.com/download) (or the version matching `TargetFramework` in `NuGetImpactAnalyzer.csproj`).

## Build and run

From the repository root:

```powershell
dotnet build "src\NuGetImpactAnalyzer.sln" -c Release
dotnet run --project "src\NuGetImpactAnalyzer\NuGetImpactAnalyzer.csproj" -c Release
```

Unit tests:

```powershell
dotnet test "src\NuGetImpactAnalyzer.sln" -c Release
```

## Solution layout

- `src/NuGetImpactAnalyzer/` — WPF application.
- `tests/NuGetImpactAnalyzer.Tests/` — automated tests.

Major dependencies include **LibGit2Sharp** (Git), **NuGet.Versioning** (range satisfaction), **CommunityToolkit.Mvvm**, and **Microsoft.Extensions.DependencyInjection**.
