# CLAUDE.md — ArcaPhylax

## Project Overview

ArcaPhylax is a .NET 10 Avalonia 12 desktop application for local media and document management.
It uses an MVVM architecture with Autofac 9 for dependency injection, SQLite + Dapper for local
storage, and an `AssemblyLoadContext`-based plugin system that loads isolated plugin assemblies
at runtime. PostgreSQL provisioning infrastructure exists for future use. Funcky 3.6.0 is the
functional programming library in use throughout the codebase.

The core is split into four layers so the host executable is only the UI shell and everything a
plugin can depend on lives in a standalone DLL:

Solution layout:

```
ArcaPhylax/                  Avalonia UI shell (App, shell + feature VMs/Views, CompositionRoot,
                             plugin loader). References Abstractions + Plugin.Sdk + Infrastructure.
ArcaPhylax.Abstractions/     Pure contracts + domain records (ITabContentViewModel, IConfigurationSection,
                             IPluginContext, I*Repository, Content/* records). ZERO package dependencies.
ArcaPhylax.Plugin.Sdk/       Plugin-authoring surface: IPlugin, ViewModelBase. Refs Abstractions +
                             Autofac + CommunityToolkit.Mvvm. Plugins reference THIS (not Abstractions).
ArcaPhylax.Infrastructure/   Persistence + provisioning: SQLite/Dapper repositories, DbInitializer,
                             indexing queue/worker, Postgres provisioning, InfrastructureModule. No Avalonia.
ArcaPhylax.PluginPackaging/  The .arcaplugin format: plugin.json manifest, signing/verification,
                             compatibility check, per-user PluginStore. No Avalonia. Used by host + tool.
tools/ArcaPhylax.PluginPack/ Developer tool: `keygen`, `pack` (publish + sign + verify, `--catalog` adds to catalog.json).
site/                        Static website (homepage, plugins page, docs) for GitHub Pages.
.github/workflows/pages.yml  Builds site/ + packs/signs every catalog plugin + catalog.json, deploys to Pages.
Plugins/FractalPlugin/       Plugin example (with tabs, FractalPluginModule)
Plugins/ScannerPlugin/       Plugin example (NAPS2 scanner workflow)
```

Dependency direction: Abstractions ← everyone; Infrastructure and Plugin.Sdk ← host. Plugins depend
only on Plugin.Sdk (which brings Abstractions transitively) and never reference Infrastructure or the
host — they consume repository *interfaces* from Abstractions and receive the Infrastructure
implementations through the host's child Autofac scope.

---

## Commands

```bash
# Build (also publishes all plugins into bin/plugins/)
dotnet build ArcaPhylax.sln

# Run
dotnet run --project ArcaPhylax/ArcaPhylax.csproj

# Run all tests (Microsoft Testing Platform, enabled via "test" runner in global.json)
dotnet test --solution ArcaPhylax.sln

# Run tests for a specific project
dotnet test --project Plugins/NutritionPlugin.Tests/NutritionPlugin.Tests.csproj
```

---

## Functional Programming Conventions

These rules apply everywhere. Do not deviate without an explicit comment explaining why.

### Immutability

- Use `record` for all data-carrying types. Prefer positional records: `record Foo(int X, string Y)`.
- Use `init`-only setters when a full positional record is awkward: `public string Name { get; init; }`.
- Use `ImmutableDictionary`, `ImmutableList`, `ImmutableArray` for collections that must not change
  after construction. See `DocumentRepository` and `DroppedItemsRepository` for the established
  `ImmutableDictionary<string, object?>.Empty.Add(...)` pattern for Dapper parameters.
- Never mutate a collection in place if you can return a new one instead.

### Pure Functions

- Domain logic (parsing, filtering, mapping, validation) must be pure: no `DateTime.Now`,
  no file I/O, no database calls, no logging inside domain methods.
- Push all I/O to the outer shell: repositories, ViewModels, hosted services.
- A method is suspicious if it has a `void` return and is not a constructor, event handler,
  or explicit side-effect boundary. Consider returning a value instead.

### Funcky Usage

Import `using static Funcky.Functional;` for `Fn`, `Compose`, `Pipe`, `NoOperation`, `Unit`.

| Situation | Use |
|---|---|
| Value that may be absent | `Option<T>` — never `null` for domain types |
| Operation that can fail with a typed error | `Result<T, TError>` |
| Lazy sequence operations | `Sequence.Return`, `Sequence.Concat`, `WhereSelect` |
| Pipeline composition | `Fn(...).Compose(Fn(...))` — see `PluginManager._toPluginDllComposition` |
| Filtering and mapping in one pass | `.WhereSelect(fn)` over `.Where().Select()` |

Do not use `Option<T>` for booleans or for things that are always present.
Do not wrap infrastructure exceptions in `Result` unless the caller is expected to handle them.

### Expressions Over Statements

Prefer expression forms at every level:

- Switch expressions over switch statements.
- Pattern matching (`is`, `switch`) over chains of `if/else if`.
- Ternary `condition ? a : b` for simple two-branch returns.
- Expression-bodied members for single-expression methods and properties:
  `public string Title => "Documents";`
- LINQ method chains over `foreach` + mutation when the result is a new collection.

Do not use expression bodies when the expression is long enough to harm readability.

### Avoid Mutation

- Never write `var x = defaultValue; if (condition) x = otherValue;`. Use ternary or switch.
- Never assign to a local variable more than once unless it is an accumulator with a clear name
  and an obvious reason (e.g., `for` loop counter, `StringBuilder`).
- Never reassign method parameters.

### Small Composable Functions

- A method should do one thing. If you need a comment to separate two conceptual steps, split
  the method.
- Private static methods are preferred over private instance methods when they do not need `this`.
  See `PluginManager.PluginName`, `ToPluginDllOrNone`, `PluginDllFromName`.
- Name functions after what they return, not what they do: `PluginName(dir)` not `GetPluginName`.
- Compose small functions via `Fn(...).Compose(...)` rather than nesting calls.

### Composition Over Inheritance

- All concrete types are `sealed` unless there is a specific, documented reason to allow subclassing.
- Extend behaviour via interfaces and constructor injection, not via base class overrides.
- The only permitted base class in domain/ViewModel code is `NotifyBase` (INPC plumbing only).
  Do not add logic to `NotifyBase`.

---

## TDD / Red-Green-Refactor Conventions

Every test project must follow these rules from its first commit.

### Test Stack

- **Test runner:** xUnit v3 (`xunit.v3`) on Microsoft Testing Platform v2. Test projects set
  `<OutputType>Exe</OutputType>`; do not add `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio` (VSTest).
- **Funcky assertions:** `Funcky.Xunit.v3` — use its static extension members on xUnit's `Assert`
  (`Assert.Some(option)`, `Assert.Some(expected, option)`, `Assert.None(option)`, `Assert.Ok(result)`,
  `Assert.Error(result)`, `Assert.Left(either)`, `Assert.Right(either)`) instead of unwrapping by hand.
  They live in `Funcky.Extensions`, which `FunckyImplicitUsings` already imports. Do not use the legacy
  `FunctionalAssert` class or reference the old `Funcky.Xunit` package. The package also registers
  xUnit serializers so `Option`/`Either`/`Unit` can be used as `[Theory]` data.
- **Property-based testing:** `FsCheck.Xunit.v3` for invariant/generative tests
- **In-memory SQLite:** `Microsoft.Data.Sqlite` with `DataSource=:memory:` for repository tests
- **Mocking (last resort):** NSubstitute, only when a hand-written fake would require implementing more than five members

### Project Naming

Test projects mirror the source project they test with a `.Tests` suffix and live adjacent in the solution:

| Source project | Test project |
|---|---|
| `ArcaPhylax` | `ArcaPhylax.Tests` |
| `ArcaPhylax.Abstractions` | `ArcaPhylax.Abstractions.Tests` |
| `Plugins/FractalPlugin` | `Plugins/FractalPlugin.Tests` |
| `Plugins/ScannerPlugin` | `Plugins/ScannerPlugin.Tests` |

Add every test project to `ArcaPhylax.sln`.

### Red-Green-Refactor — the non-negotiable sequence

1. **Red.** Write one failing test that describes a single behaviour. Run it; confirm it fails for
   the right reason (not a compile error). Commit the failing test alone so the history proves
   the test existed before the implementation.

2. **Green.** Write the minimum production code that makes the test pass. Do not write code not
   demanded by a currently failing test. No extra overloads, edge-case handling, or logging unless
   a test forces it.

3. **Refactor.** Clean up duplication, rename for clarity, extract private methods — all while
   keeping the test suite green. Never change test assertions during refactor. If a refactor breaks
   a test, the refactor is wrong.

Repeat the cycle for each new behaviour.

### Test Naming

Use the format: `MethodOrBehaviour_GivenCondition_ExpectedOutcome`

```csharp
QueryTiles_WhenSearchIsNull_ReturnsAllDocuments()
QueryTiles_WhenSearchMatchesTitle_ReturnsMatchingDocuments()
QueryTiles_WhenLimitIsZero_ReturnsEmptySequence()
Enqueue_WhenJobAdded_JobIsReadableFromChannel()
ToViewTypeName_GivenViewModelTypeName_ReplacesPostfix()
```

Avoid generic names like `Test1`, `WorksCorrectly`, or `HappyPath`.

### Test Structure

Each test must follow Arrange / Act / Assert with a blank line between each phase:

```csharp
[Fact]
public void QueryTiles_WhenSearchIsNull_ReturnsAllDocuments()
{
    // Arrange
    var db = BuildInMemoryDb();
    SeedDocuments(db, count: 3);
    var sut = new DocumentRepository(db);

    // Act
    var result = sut.QueryTiles(search: null, limit: 100, offset: 0).ToList();

    // Assert
    Assert.Equal(3, result.Count);
}
```

No logic inside `Assert` blocks — compute expected values in the Arrange section.
Aim for one logical assertion per test. Use `Assert.Multiple` only when grouping assertions on a
single returned object.

### What to Test

Test in this priority order:

1. **Pure domain functions** — anything with no I/O. These have no external dependencies.
2. **Repositories** — test via an interface backed by `:memory:` SQLite. Run
   `DbInitializer` (SQLite schema only) on the in-memory connection before each test class.
3. **ViewModels** — instantiate directly with fake/mock implementations of interfaces. No Avalonia
   rendering needed. Test observable property changes by subscribing to `PropertyChanged`.
4. **Property-based tests** — use `[Property]` from FsCheck.Xunit.v3 for functions where invariants
   hold for all inputs (e.g., round-trip serialisation, sorting stability, Funcky `Option` laws).

### What NOT to Test

Do not test:

- Avalonia XAML rendering, controls, or bindings.
- `PluginLoadContext` / `AssemblyLoadContext` loading mechanics.
- `CompositionRoot.BuildContainer()` DI wiring.
- PostgreSQL provisioning infrastructure (`PostgresProvisioner`, `PostgresDetector`, etc.).

These are integration concerns. If coverage of them becomes necessary, isolate them behind
testable interfaces first, then test the interface, not the infrastructure class.

### Fakes vs Mocks

Prefer hand-written fakes over mocking frameworks. Keep fakes in a `Fakes/` folder within the
test project. Use NSubstitute only when a hand-written fake would require implementing more than
five members.

---

## Architecture Reminders

### Plugin Isolation

`PluginLoadContext` resolves assemblies from the plugin's own output directory. The following
assemblies are always resolved from the host's default load context (shared):

- `ArcaPhylax.Abstractions`
- `ArcaPhylax.Plugin.Sdk`
- `CommunityToolkit.Mvvm`
- `Autofac`
- Any assembly whose name starts with `Avalonia`

`CommunityToolkit.Mvvm` is a deliberate exception to the "each plugin carries its own copy" rule:
its source-generated `[ObservableProperty]`/`[RelayCommand]` setters bind to whichever
`ObservableObject` the plugin compiled against, so a second copy in the plugin's own context gives a
different type identity and throws `MethodAccessException` across the ALC boundary. Sharing one copy
lets plugin VMs safely derive from `ViewModelBase` (in `ArcaPhylax.Plugin.Sdk`).

Do not add Funcky or other runtime libraries to the shared list — each plugin carries its own copy.
If a plugin needs a type to cross the plugin/host boundary, that type must live in
`ArcaPhylax.Abstractions` (contracts/records) or `ArcaPhylax.Plugin.Sdk` (authoring base types).

### Plugin Packaging & Installation

- **Slim output.** `Plugins/Directory.Build.targets` makes host-provided packages (Avalonia, Autofac,
  CommunityToolkit.Mvvm, SkiaSharp, HarfBuzzSharp, …) compile-only and drops foreign `runtimes/<rid>/`
  folders. A plugin's publish output must contain only its OWN files; never re-add host assemblies.
- **Metadata** lives in the plugin `.csproj`: `PluginId`, `Title`, `Description`, `Version` (`Authors`
  defaults in `Plugins/Directory.Build.props`). `ArcaPhylax.Plugin.Sdk`'s `<Version>` is the plugin
  contract version: bump the MAJOR on breaking changes to `IPlugin`/Abstractions.
- **Packages** (`.arcaplugin`, one per platform): zip with `plugin.json`, `payload/`, `signature.json`
  (ECDSA P-256 over the sorted SHA-256 listing of every entry, plus the signer's public key). Build with
  `dotnet run --project tools/ArcaPhylax.PluginPack -- pack Plugins/X/X.csproj --key <file>`.
  Private keys never enter the repo (`*.key` is git-ignored).
- **Trust** (`TrustPolicy`): verifying a package only proves WHO signed it; `TrustPolicy.Check(id, signer)`
  decides whether that signer may publish that id. Sources: `TrustAnchors.BuiltIn` (first-party publisher,
  prefix `ArcaPhylax`) + `publishers.json` (`PublisherList`, signed by the offline root key
  `TrustAnchors.Root`, fetched next to the catalog, cached in `%LOCALAPPDATA%\ArcaPhylax`, never replaced
  by a lower `Sequence`) + the user's overrides (`UserTrust`, `trust.json`). Revocation beats everything;
  a user block beats an endorsement; a user-trusted key cannot claim an id an endorsed publisher owns.
  Installed plugins record their signer (`signer.json`) and are re-checked at every start. Edit the
  list locally with `pluginpack endorse|revoke site/publishers.json --root <root.key> ...` — the root key
  never goes to CI. Signers are matched by full public key, not just the 16-char key id.
- **Install** goes to the per-user `PluginStore` (`%LOCALAPPDATA%\ArcaPhylax\plugins\<id>\<version>\`).
  New plugins load live; updates/removals are staged in `pending.json` and applied at startup before
  any plugin loads. An installed plugin replaces a bundled one with the same entry assembly.
- **Catalog** (`catalog.json`, `PluginCatalog` in PluginPackaging) is published to GitHub Pages
  (`https://freeapophis.github.io/ArcaPhylax/`, `PluginHost.CatalogUri`; override with the
  `ARCAPHYLAX_PLUGIN_CATALOG` env var, URL or local path). The Plugins page offers installs/updates via
  `PluginOffer`; downloads must match the catalog's size + SHA-256, then go through the normal signed install.
  CI packs each plugin for its `PluginPlatforms` (default: all RIDs); `<PluginInCatalog>false</PluginInCatalog>`
  keeps a plugin bundled-only. Releasing = bump the plugin's `<Version>` and push to main.

### Plugin Registration Pattern

Every plugin that contributes tabs must follow the `FractalPlugin` pattern:

1. Create an Autofac `Module` subclass (e.g., `MyPluginModule`).
2. In `Load(ContainerBuilder)`, register the tab ViewModel as `ITabContentViewModel` (and `AsSelf`
   if used internally), and register the corresponding View type.
3. Do NOT add `WithMetadata("Source", "Host")` to plugin registrations — that metadata is reserved
   for host tabs.
4. Call `builder.RegisterModule<MyPluginModule>()` from `IPlugin.Configure`.

### ViewFactory Naming Contract

`ViewFactory.Create(viewModel)` maps a ViewModel type to a View type by replacing the `ViewModel`
suffix with `View` in the fully-qualified type name. Both must be in the same assembly and share
the same namespace prefix.

- `FractalPlugin.ViewModels.FractalViewModel` → `FractalPlugin.Views.FractalView`
- `ArcaPhylax.ViewModels.DocumentsViewModel` → `ArcaPhylax.Views.DocumentsView`

The View must be registered in Autofac so that `PluginManager.Resolve(viewType)` can find it.

### INPC / ViewModel Rules

- All ViewModels inherit `NotifyBase` from `ArcaPhylax.Abstractions`.
- Use `Set(ref _backing, value)` for properties that need change notification.
- Use `sealed record` for pure display models (e.g., `DocumentTileVm`). These are immutable
  projection types, not ViewModels, and do not inherit `NotifyBase`.

### Dapper Parameter Pattern

Always build Dapper parameters with `ImmutableDictionary<string, object?>`:

```csharp
var parameters = ImmutableDictionary<string, object?>.Empty
    .Add("key", value)
    .ToDynamicParameters();  // extension method from ArcaPhylax.Extensions
```

Never pass anonymous objects to Dapper queries. The immutable dictionary pattern makes parameter
sets composable.
