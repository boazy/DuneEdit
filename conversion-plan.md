# Dune Editor .NET and Cross-Platform Conversion Plan

## Goal

Convert the `DuneEdit.sln` applications from .NET Framework 4.0 and Windows Presentation Foundation (WPF) to the latest stable .NET release and a cross-platform desktop UI. Preserve the save-game format and the current open, edit, save, map, and location-detail behavior on Windows, macOS, and Linux.

As of 2026-08-08, the target is **.NET 10**, the latest stable release and an active Long Term Support (LTS) release. .NET 11 is still a preview and is not a release target.

## Implementation status

The conversion was implemented on 2026-08-08. The repository now uses the .NET 10 SDK managed by `mise`, Avalonia 12.1.1, and the project structure described below.

Use these commands from a clean clone:

```sh
mise install
mise run test
mise run run
```

To open a file at startup:

```sh
mise exec -- dotnet run --project src/DuneEdit.Desktop -- /path/to/DUNE21S0.SAV
```

The GitHub Actions workflow in `.github/workflows/ci.yml` builds, tests, and starts the desktop application on Windows, macOS, and Linux; the Linux startup check runs under Xvfb. It also publishes self-contained `win-x64`, `osx-arm64`, and `linux-x64` artifacts for the desktop editor and `F7`.

Every publish job executes the self-contained desktop binary with `DOTNET_ROOT` pointed at a nonexistent directory. The binary generates a temporary save, opens it through the desktop view model, changes an editable location field, saves it, reparses it, and reopens it. Linux runs the same artifact scenario under Xvfb.

Compatibility checks covered all five local floppy saves, an editor-written floppy save loaded by the original game in DOSBox Staging, and the CD release's `DNCDPRG.EXE`. The five floppy saves and the CD executable each retained byte-identical output when opened and serialized without edits.

## Assessed baseline

| Area | Repository evidence | Conversion impact |
| --- | --- | --- |
| Build | `DuneEdit.sln` is a Visual Studio 2010 solution. `DuneEdit.csproj` and `Tools/F7/F7.csproj` target .NET Framework 4.0 and `x86`. | Replace both projects with SDK-style `net10.0` projects and remove the x86-only solution configuration. |
| UI | `DuneEdit` uses WPF XAML, `PresentationFramework`, `System.Windows`, WPF commands, navigation, dialogs, animation, and image APIs. | Updating the target framework alone cannot make the application cross-platform. The UI framework and platform services must be ported. |
| Build extensions | The main project runs `NotifyPropertyWeaverMsBuildTask.dll` after compilation and generates `SelectionEffect.cs` from a WPF shader. | Remove both legacy build hooks. Replace property notification and selection highlighting with supported source code and portable UI behavior. |
| Core logic | Compression, save parsing, location models, bit fields, and lookup data mostly use portable base-class-library APIs. | Extract this code before porting the UI so its binary behavior can be tested independently. |
| Compression | `DuneEdit/Compression.cs` and `Tools/F7/Compression.cs` duplicate the algorithm but disagree about six-byte versus seven-byte header handling. | Determine the correct behavior from representative files, then keep one implementation shared by the desktop app and `F7`. Do not choose one copy without file-based evidence. |
| Tests and samples | The repository has no test project and contains no `.sav` fixture. | Obtain representative compressed save files and executable-format inputs before changing binary logic. Store redistributable fixtures or deterministic, documented test vectors. |
| Assets | The map symbols are PNG resources under `DuneEdit/Res`. | Reuse the images as Avalonia resources. Validate resource names on case-sensitive Linux file systems. |
| Local prerequisites | The assessment workstation does not currently have `dotnet` on `PATH`. | Install the .NET 10 SDK before implementation and pin the accepted SDK feature band in `global.json`. |

The primary scope is the two .NET projects in `DuneEdit.sln`: the desktop editor and `F7`. `Tools/Hsq2Png` and `Tools/unhsq.cpp` are standalone C++ utilities and are not required by the editor at runtime. Porting those utilities is a separate workstream unless a release dependency is discovered.

## Target architecture

Use **Avalonia UI** for the desktop application. Avalonia is a XAML-based .NET UI framework with supported desktop targets for Windows, macOS, and Linux. It provides the closest practical migration path for the current WPF layout while avoiding three platform-specific UI implementations.

Create this project structure:

```text
DuneEdit.sln
src/
  DuneEdit.Core/       net10.0 library: save format, compression, models, lookup data
  DuneEdit.Desktop/    net10.0 Avalonia executable: views, view models, platform services
  F7/                  net10.0 console executable referencing DuneEdit.Core
tests/
  DuneEdit.Core.Tests/ binary-format and model behavior
```

Apply these boundaries:

- `DuneEdit.Core` must not reference Avalonia, WPF, operating-system dialogs, or other UI APIs.
- `DuneEdit.Desktop` owns file pickers, clipboard access, dialogs, images, animation, and window state.
- `F7` and `DuneEdit.Desktop` must call the same compression implementation.
- Use `AnyCPU` for managed builds. Produce Runtime Identifier (RID)-specific self-contained release artifacts instead of compiling the source as x86.
- Pin .NET and package versions. Dependabot or a similar tool may propose updates, but builds must remain reproducible from committed project files and lock data.

## Migration phases

### 1. Establish a behavioral baseline

- Install the .NET 10 SDK on development and CI machines.
- Build and run the existing editor in a Windows environment that supports .NET Framework 4.0.
- Capture the current workflows: open a save, draw and select map locations, edit numeric and Boolean fields, save, close, and reload.
- Collect representative compressed `.sav` files and executable-format inputs. Include edge cases for the F7 marker byte, maximum repeat counts, and files with short or malformed sequences when such files are valid test subjects.
- Record known pre-existing gaps, including the currently unbound **Save As** menu entry, separately from conversion defects.
- Resolve the six-byte/seven-byte compression discrepancy by comparing both implementations against known files and game-compatible output.

**Gate:** Expected binary outputs, parsed location values, and visible workflows are documented well enough to detect a conversion regression.

### 2. Modernize the build and isolate the core

- Replace the legacy solution and C# project files with SDK-style projects targeting `net10.0`.
- Add `global.json` for the accepted .NET 10 SDK feature band and centralize common compiler settings.
- Move save parsing, compression, bit-field code, regions, location sequences, and models into `DuneEdit.Core` without changing file-format behavior.
- Consolidate the compression code and make `F7` reference `DuneEdit.Core`.
- Replace obsolete assembly metadata, `app.config`, generated settings/resources files, and explicit framework references with SDK-style properties and resource declarations.
- Add focused tests for known compression vectors, `Decompress(Compress(raw))`, location offset discovery, field-to-byte mappings, status-bit mappings, load-edit-save-reload behavior, and unchanged bytes outside edited fields.

**Gate:** Core and CLI projects build on Windows, macOS, and Linux. Every approved fixture produces the expected parsed values and round-trip output.

### 3. Replace WPF with Avalonia

- Create the Avalonia application shell and port `App.xaml`, `MainWindow.xaml`, and `SietchDetailsPage.xaml` to Avalonia XAML.
- Keep the current layout and workflow. Avoid a visual redesign during the framework migration.
- Replace WPF-only behavior:
  - Use Avalonia commands or view-model commands for Open and Save.
  - Use Avalonia `StorageProvider` APIs for file selection.
  - Use Avalonia clipboard and dialog services instead of `System.Windows` and `Microsoft.Win32` APIs.
  - Replace WPF `BitmapImage`, `Image`, `Canvas`, `GridSplitter`, and navigation usage with Avalonia equivalents.
  - Replace the single-page WPF `Frame` with a direct content/view binding unless real navigation is required.
  - Replace height/width animation with a render transform or Avalonia transition so map layout is not repeatedly recalculated.
- Replace the WPF pixel shader with a portable selection treatment, such as scale plus an outline or drop shadow. Compare the result on all three platforms; do not carry forward the generated shader code or `.ps` binary.
- Remove the binary property-change weaver. Implement explicit observable view models or source-generated property notification using a maintained, cross-platform package.
- Preserve map coordinate calculations, location image selection, detail bindings, and save semantics. Keep all byte-level work in `DuneEdit.Core`.

**Gate:** On each target operating system, a user can launch the app, open a fixture, view and select every location type, edit representative fields, save, reopen, and observe the same values.

### 4. Add cross-platform CI and release artifacts

- Run restore, build, and core tests on Windows, macOS, and Linux CI runners.
- Publish self-contained artifacts for these primary targets:
  - Windows x64: `win-x64`
  - Apple Silicon macOS: `osx-arm64`
  - Linux x64: `linux-x64`
- Add `win-arm64`, `osx-x64`, and `linux-arm64` when those architectures are part of the supported release matrix. Do not claim support for an artifact that has only compiled and has never been launched.
- Start with self-contained directory archives. Add installers, signing, notarization, AppImage, Flatpak, or distribution packages only after the unpacked artifacts pass launch and workflow checks.
- Run an operating-system smoke check against every release candidate. On Linux, test with the documented Avalonia native dependencies installed.
- Keep platform-specific code behind narrow services. The save format, editor rules, and view models must use the same implementation on every operating system.

**Gate:** CI produces artifacts from one commit, and the primary Windows, macOS, and Linux artifacts pass the same open-edit-save-reload scenario without requiring a preinstalled .NET runtime.

### 5. Remove legacy paths and document operation

After the cross-platform smoke checks pass:

- Delete the WPF project, WPF-only XAML/code, generated `SelectionEffect.cs`, shader generator inputs and output, `app.config`, old generated settings/resources code, and `NotifyPropertyWeaverMsBuildTask.dll`.
- Remove duplicated compression code, x86-only configurations, stale assembly metadata, dead commented code, and unused imports.
- Confirm that no project references `System.Windows`, `PresentationCore`, `PresentationFramework`, `WindowsBase`, or the legacy build task.
- Document supported operating systems and architectures, prerequisites, local build/run commands, fixture policy, release commands, Linux native dependencies, and known limitations.

**Gate:** A clean clone contains one active implementation for each behavior and no legacy compatibility path is needed to build or run the converted applications.

## Verification matrix

| Capability | Automated evidence | Manual release evidence |
| --- | --- | --- |
| Compression | Known vectors and raw-data round trips | Open compressed output in the original game or another trusted implementation. |
| Parsing | Fixture-to-model assertions for locations and fields | Compare representative values with the legacy editor. |
| Editing | Byte-level assertions that expected offsets change and unrelated bytes do not | Edit numeric and Boolean fields, save, reopen, and inspect them. |
| Map | View-model tests for coordinate and image selection logic | Resize the window, select each location type, and verify details and selection highlighting. |
| Files | Core tests for load/save behavior and error cases | Open and save through native pickers on each operating system. |
| Packaging | CI builds and publishes every declared RID | Launch each artifact on a clean or representative machine without the .NET SDK. |

The primary launch baseline should cover a current supported Windows x64 system, current Apple Silicon macOS, and a current glibc-based Linux x64 distribution. Additional operating-system versions and CPU architectures become supported only when they are listed in the release matrix and exercised regularly.

## Main risks and controls

- **Binary corruption:** Save files use fixed offsets and custom compression. Freeze behavior with fixtures and byte-level tests before refactoring.
- **Unknown compression header rule:** The two current implementations disagree. Settle the rule with real files and compatibility checks, not code preference.
- **Implicit property notification:** The current UI depends on a legacy post-compile weaver. Make notification explicit and test that edits update both the view and the underlying bytes.
- **Rendering differences:** WPF shader and animation behavior will not port exactly. Preserve the user-visible selection state with simpler portable primitives and verify it visually on each operating system.
- **Platform support drift:** Avalonia support tiers and operating-system minimums change. Record exact supported versions in release documentation and review them when updating Avalonia.
- **Packaging complexity:** Signing and native package formats can delay functional validation. Prove self-contained published directories first, then add native packaging.
- **Scope expansion:** UI redesign, reverse engineering unknown save fields, mobile/web targets, and conversion of standalone C++ tools are outside this migration unless separately approved.

## Definition of done

The conversion is complete when all of the following are true:

- The solution targets the pinned .NET 10 SDK and builds from a clean clone on Windows, macOS, and Linux.
- The desktop application uses Avalonia and contains no WPF or Windows-only UI dependency.
- `DuneEdit.Desktop` and `F7` share one tested core implementation.
- Approved save fixtures parse, edit, save, and reload without unintended byte changes.
- The desktop workflow passes on the three primary operating-system targets.
- CI publishes self-contained artifacts that launch without a preinstalled .NET runtime.
- Legacy build tasks, generated shader code, duplicate compression code, x86-only configuration, and obsolete .NET Framework files are removed.
- Build, run, support, and release instructions match the verified commands and supported platform matrix.

## Reference choices

- [.NET downloads and supported versions](https://dotnet.microsoft.com/en-us/download/dotnet): identifies .NET 10 as the latest stable LTS release as of this assessment.
- [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms): documents Windows, macOS, and Linux desktop support and platform-specific runtime requirements.
