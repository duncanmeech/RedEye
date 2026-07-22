# RedEye — Enhancements & Modernization

This document is a companion to `design.MD`. It describes two related but
independent lines of work:

1. **Modernization** — moving RedEye from **.NET Framework 4.0 / WinForms /
   x86** onto the **latest .NET (currently .NET 10 LTS)** in a staged,
   low‑risk way.
2. **Incremental performance improvements** — a prioritized list of changes
   that can be applied one at a time, each with a clear cost/benefit and
   without requiring a full rewrite.

The two tracks intentionally overlap: several of the largest performance wins
(spans, SIMD, hardware intrinsics, `Parallel.For` scaling) only become
available *after* the framework is modernized, so the recommended order is
"lift the runtime first, then optimize."

---

## 1. Current State (from `design.MD`)

For context, the properties that most influence this plan:

- **Target framework:** `.NET Framework 4.0` (`RedEye.csproj`, `app.config`)
- **Project format:** Legacy (non‑SDK) MSBuild `.csproj`, `ProjectGuid`,
  explicit `<Reference Include="System.Drawing" />`, etc.
- **Platform target:** `x86`, with `AllowUnsafeBlocks` in `Debug|x86`
- **UI:** `System.Windows.Forms`, single `MainForm` used purely as a demo
  harness
- **Dependencies:** none — only BCL assemblies
- **Hot paths:**
  - Nested `for (y) for (x)` loops using `Bitmap.GetPixel` / `SetPixel`
    (`RedEyeTool.GetCIELABImage`, `GetMaskImage`, `GetNewImage`,
    `GetBitmapFromCIELABImage`)
  - Per‑pixel color‑space math in `RgbaHls` returning `double` values,
    boxed through a heap‑allocated `CIELab` class
  - 2D convolution / Gaussian blur in `ImageUtility.Convolve`
  - Iterative connected‑component flood‑fill in `BlobMap` using
    `Stack<Point>`
- **Error handling:** the whole pipeline is wrapped in
  `try { … } catch (Exception) { }` and silently returns a possibly‑`null`
  bitmap on failure.

Every recommendation below is anchored in one of these facts.

---

## 2. Modernizing to the Latest .NET

### 2.1 Target version

As of mid‑2026 the current **LTS** release is **.NET 10** (GA November 2025,
supported through late 2028). Unless there is a specific reason to be on the
STS train, **target `net10.0-windows` for the WinForms harness** and
`net10.0` (no OS suffix) for any code that can be made platform‑agnostic.

WinForms itself is a Windows‑only workload in modern .NET — the harness will
remain Windows‑only, but as noted in §2.5 the *image processing library*
can be made cross‑platform with a different imaging backend.

### 2.2 Staged migration plan

The safest path is a sequence of small, individually verifiable steps rather
than a single "big‑bang" retarget.

#### Stage 0 — Baseline & safety net

Before touching the project file:

1. **Capture the current behavior as a golden test.** Add a small console or
   xUnit project that loads `redeye.png`, runs `RedEyeTool.ProcessImage`,
   and hashes the resulting bitmap (e.g., SHA‑256 of the raw pixel bytes
   from `LockBits`). Every subsequent migration step must reproduce this
   hash *exactly* (or, once floating‑point ordering changes, be within a
   small per‑channel tolerance).
2. **Add a micro‑benchmark.** Even a `Stopwatch`‑based console runner that
   averages 20 iterations of `ProcessImage` on the sample image is enough to
   detect regressions or confirm wins after each step.
3. **Commit the current binary output** (bin/Release) as a reference build
   so behavior can be A/B compared later.

#### Stage 1 — Convert to SDK‑style `.csproj`

The current project file (`RedEye.csproj`) is the pre‑2017 non‑SDK format
(`ToolsVersion="4.0"`, `<ProjectGuid>`, explicit `<Compile Include="…">` for
every file). Convert it to the modern SDK style *while still targeting
.NET Framework 4.0*. This is intentionally a no‑op change to behavior.

Target file, roughly:

```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net40</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>RedEye</RootNamespace>
    <AssemblyName>RedEye</AssemblyName>
    <PlatformTarget>x86</PlatformTarget>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <LangVersion>latest</LangVersion>
    <Nullable>disable</Nullable>
  </PropertyGroup>
</Project>
```

Notes:

- `Microsoft.NET.Sdk.WindowsDesktop` is the SDK for WinForms/WPF apps.
- `<UseWindowsForms>true</UseWindowsForms>` replaces the explicit
  `System.Windows.Forms` / `System.Drawing` `<Reference>` entries.
- The SDK auto‑globs `**/*.cs`, so all the `<Compile Include="…">` lines
  disappear. `.resx` files are also auto‑globbed with the right generator.
- Keep `PlatformTarget=x86` for now — Stage 3 revisits that.
- Delete `app.config` (or keep only entries that are still meaningful; the
  `<supportedRuntime>` element is no longer relevant).

Validate: rebuild, run harness, confirm golden‑hash still matches.

#### Stage 2 — Retarget to .NET 8 (transitional)

**Why not jump straight to .NET 10?** Retargeting is one variable at a time.
.NET 8 is still supported (LTS through Nov 2026) and is the point at which
most legacy‑framework issues surface (analyzer, `System.Drawing.Common`,
implicit `using` semantics). Once .NET 8 is green, the .NET 10 bump is
essentially a one‑line change.

Change:

```xml
<TargetFramework>net8.0-windows</TargetFramework>
```

Expected friction:

- **`System.Drawing.Common` is a NuGet package** and, since .NET 6, is
  supported **only on Windows**. Add:
  ```xml
  <PackageReference Include="System.Drawing.Common" Version="8.*" />
  ```
  (or use `Microsoft.Windows.Compatibility` if any other Windows‑only APIs
  creep in). On non‑Windows targets this package throws
  `PlatformNotSupportedException`, which is why §2.5 recommends splitting
  the imaging code out entirely.
- **`Bitmap`, `ColorMatrix`, `PictureBox`** all continue to work on Windows
  without code changes.
- **Removed BCL surface.** Nothing in the current source appears to use
  APIs removed between .NET Framework 4.0 and .NET 8, but do a build and
  address any warnings/errors one at a time. Common cases:
  - `Hashtable`/`ArrayList` still work but should be replaced by
    `Dictionary<TKey,TValue>` / `List<T>` (see §3.6).
  - `[Serializable]` on classes that don't participate in binary
    serialization can be deleted; `BinaryFormatter` is deprecated and
    should not be used.
- **Culture‑sensitive parsing.** Any `double.Parse` / `ToString` without a
  `CultureInfo` should be reviewed. The RedEye code does not currently
  parse strings, so this is likely a non‑issue.

Validate: rebuild, run harness, re‑run golden test + benchmark.

#### Stage 3 — Move to .NET 10 LTS and drop `x86`

Once .NET 8 is stable:

```xml
<TargetFramework>net10.0-windows</TargetFramework>
```

Also:

- **Change platform target from `x86` to `AnyCPU`** (or `x64`). The `x86`
  target is a legacy artifact — nothing in the code requires a 32‑bit
  process, and 64‑bit gives more address space (helpful once we stop being
  size‑capped at `2048 × 2048`, see §3.7) and access to a wider register
  file for SIMD.
- Bump `System.Drawing.Common` to the matching major version.
- Enable modern C# language features project‑wide:

  ```xml
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  ```

- Add analyzers: `Microsoft.CodeAnalysis.NetAnalyzers` is on by default in
  modern SDKs; consider also `Meziantou.Analyzer` and
  `Roslynator.Analyzers` for stylistic cleanups.

Validate: build, harness, golden test, benchmark.

#### Stage 4 — Ready for STS bumps

After Stage 3, keeping current is a yearly retarget:

- **.NET 11** (STS, expected Nov 2026) — usually a `<TargetFramework>` bump
  plus running the "Upgrade Assistant" once.
- **.NET 12 LTS** (expected Nov 2027) — the next long‑support target.

Because every prior stage was small and reversible, subsequent yearly bumps
should be minutes of work, not days.

### 2.3 Tooling

- **[.NET Upgrade Assistant]** (`dotnet tool install -g
  upgrade-assistant`) can automate Stages 1–2 in one shot, but it is worth
  doing manually the first time so the resulting `.csproj` is deliberate
  and minimal.
- **[try-convert]** is a lighter alternative that only handles the SDK‑style
  conversion in Stage 1.
- **`dotnet format`** and `dotnet build -warnaserror` in CI keep the
  modernized codebase clean going forward.

### 2.4 Editor / IDE considerations

- The current solution is Visual Studio 2010‑era (`ProductVersion
  8.0.30703`). Delete `RedEye.sln`'s `MinimumVisualStudioVersion` and let
  VS regenerate a modern one — or just rely on `dotnet build`.
- Add a `global.json` pinning the SDK band to prevent surprises:
  ```json
  { "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }
  ```

### 2.5 (Optional) Extract a cross‑platform library

The image‑processing code (`Graphics/*.cs`) has no WinForms dependency and
only depends on `System.Drawing` for the `Bitmap` type. Splitting the
project in two makes cross‑platform use possible:

```
RedEye.sln
├── RedEye.Imaging.csproj      <TargetFramework>net10.0</TargetFramework>
└── RedEye.App.csproj          <TargetFramework>net10.0-windows</TargetFramework>
                               <UseWindowsForms>true</UseWindowsForms>
                               <ProjectReference Include="..\RedEye.Imaging\..." />
```

For the library, switch the pixel container from `System.Drawing.Bitmap` to
either:

- **[SixLabors.ImageSharp]** — pure‑managed, cross‑platform, MIT‑licensed
  (commercial license required for some uses). Its `Image<Rgba32>` type is
  a near drop‑in for `Bitmap` and exposes contiguous pixel spans, which is
  a large performance win in its own right (§3.1).
- **A minimal in‑house `PixelBuffer` type** — a `struct` wrapping a
  `Memory<byte>` plus width/height/stride. Zero dependencies, but you write
  your own PNG/JPEG decode.

The Windows app project keeps the `Bitmap` I/O boundary and simply calls
into the library.

---

## 3. Incremental Performance Improvements

These are ordered by **expected ROI on the current codebase**, from highest
to lowest. Each item can be applied independently; you don't need to do
them all, and none of them require modernization to `.NET 10` as a
prerequisite *except* where explicitly noted.

For every item: **run the golden test and the benchmark** before merging.

### 3.1 Replace `GetPixel` / `SetPixel` with `LockBits` + `Span<byte>` — biggest win

**Where:** `RedEyeTool.GetCIELABImage`, `GetMaskImage`, `GetNewImage`,
`GetBitmapFromCIELABImage`, and any `ImageUtility` helper that still uses
`GetPixel`/`SetPixel`.

**Why:** `Bitmap.GetPixel` locks the bitmap, does bounds checks, converts
to a `Color` struct, and unlocks — for **every single pixel**. It is
routinely **50×–200× slower** than raw pixel access. The design doc
already flags this as "the obvious first optimization."

**How (Framework 4.0 compatible):** classical `LockBits` + `unsafe` block
with `byte*` pointers. The project already has `AllowUnsafeBlocks=true`.

**How (once on .NET 8+, much cleaner):**

```csharp
var rect = new Rectangle(0, 0, source.Width, source.Height);
var data = source.LockBits(rect, ImageLockMode.ReadOnly,
                           PixelFormat.Format32bppPArgb);
try
{
    int bytes = data.Stride * data.Height;
    var span = new Span<byte>((void*)data.Scan0, bytes);
    // iterate span in BGRA chunks of 4
}
finally { source.UnlockBits(data); }
```

`Span<T>` and `Memory<T>` remove the need for `unsafe` in most call sites
and enable the SIMD work in §3.4.

**Expected speedup on `GetCIELABImage` + `GetNewImage`:** **10×–100×** on a
typical 1‑MP crop. On the tiny embedded sample it is barely perceptible,
but on realistic inputs the pipeline goes from "seconds" to
"tens of milliseconds."

### 3.2 Make `CIELab` a `readonly struct` and stop boxing

**Where:** the `CIELab` **class** at the bottom of `Graphics/RedEyeTool.cs`
(currently a heap‑allocated reference type with three `double` fields, no
readonly), and the arrays like `CIELab[] map = new CIELab[width*height]`.

**Why:** `CIELab` is a value with no identity and no mutation semantics —
it is a textbook `struct`. As a reference type, every pixel of the CIE map
allocates a separate 24‑byte object (`3 × double`) plus 16 bytes of object
header on 64‑bit — roughly **6× the memory** of a plain `struct[]` — and
every read chases a pointer, defeating the CPU cache.

**Change:**

```csharp
public readonly struct CIELab
{
    public readonly double L;
    public readonly double a;
    public readonly double b;
    public CIELab(double l, double a, double b) { L = l; this.a = a; this.b = b; }
}
```

With that change:

- `new CIELab[…]` becomes a single contiguous 24‑bytes/pixel array
- No per‑pixel GC pressure
- Iteration is cache‑friendly, which is a prerequisite for §3.4

**Expected speedup on the CIE steps:** **2×–5×**, and dramatically lower
peak allocation.

### 3.3 Fuse pipeline stages / reuse intermediate buffers

**Where:** `ProcessImage` currently allocates and disposes six intermediate
`Bitmap`s (`rednessMask`, `rednessThreshold`, `falseColor`,
`noSmallIslands`, `noHoles`, `blured`).

**Why:** each `Bitmap.Clone()` (`falseColor = (Bitmap)rednessThreshold.Clone()`,
`noSmallIslands = (Bitmap)rednessThreshold.Clone()`) is a full pixel copy,
and each intermediate is a native GDI+ allocation.

**Change:**

- Represent intermediate binary masks as `byte[]` (or `bool[]`) buffers,
  not `Bitmap`s. Only convert back to `Bitmap` for the very last step (or
  never, if callers accept a `byte[]`/`Memory<byte>`).
- Reuse a small pool of scratch buffers: two ping‑pong buffers is
  typically enough for the convolution/threshold sequence.
- Remove the `falseColor` allocation from the production path — `design.MD`
  already notes it is a debug‑only artifact. Move it behind `#if DEBUG` or
  an opt‑in flag.

**Expected speedup:** modest on tiny inputs, but eliminates 4–6 large
allocations per call, which matters greatly for **repeated** invocations
(e.g. batch processing many crops).

### 3.4 Parallelize the pixel loops

**Where:** every `for (y) for (x)` in `RedEyeTool.cs` and `ImageUtility.cs`
that does independent per‑pixel work — which is most of them.

**Why:** modern CPUs have 8–32 cores. The per‑pixel work in
`GetCIELABImage`, `GetChromaticityDistanceMap`, `GetMaskImage`, and
`GetNewImage` is embarrassingly parallel (no cross‑pixel dependencies).

**Change (once `GetPixel`/`SetPixel` is gone — this must come after §3.1):**

```csharp
Parallel.For(0, height, y =>
{
    ReadOnlySpan<byte> row = /* pointer arithmetic into locked bytes */;
    for (int x = 0; x < width; x++) { /* … */ }
});
```

Notes:

- Parallelize **the outer loop only** (per row). The inner loop is small
  enough that parallelizing both hurts more than it helps.
- **`Bitmap.GetPixel`/`SetPixel` are NOT thread‑safe.** You must land §3.1
  first. Attempting `Parallel.For` against `GetPixel` will either
  deadlock on the internal bitmap lock or corrupt output.
- On .NET 6+, prefer
  `Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism =
  Environment.ProcessorCount }, …)` for predictable behavior.

**Expected speedup:** near‑linear in core count for the CIE/mask/blend
passes. Convolution parallelizes similarly.

### 3.5 SIMD / hardware intrinsics for hot math

**Where:** color‑space conversions (`RgbaHls.RGB_2_CIEXYZ`,
`CIEXYZ_2_CIELAB`, and their inverses), `ChromaticityDistance`, and the
inner convolution loop in `ImageUtility.Convolve`.

**Why:** these functions do the same arithmetic on every pixel. On a
modern CPU the same code, expressed with `Vector<float>` or
`System.Runtime.Intrinsics.X86` (AVX2 / AVX‑512), can process 4–16 pixels
per instruction issue.

**Options in order of complexity:**

1. **`System.Numerics.Vector<float>`** — portable, works on .NET Framework
   4.5+, but with a much richer API on .NET 6+. About 2×–4× wins with
   modest code changes.
2. **`Vector128<T>` / `Vector256<T>`** with `Sse2` / `Avx2` intrinsics —
   .NET 5+. Bigger wins, more code, requires runtime feature detection
   (`Avx2.IsSupported`).
3. **`Vector512<T>` and `Avx512`** — .NET 8+ on capable CPUs. Diminishing
   returns for this workload because the per‑pixel math is not that heavy
   and memory bandwidth becomes the bottleneck.

A practical strategy: pick one function (e.g. `ChromaticityDistance` over
a whole row) and vectorize it as a proof of concept. If the benchmark
budget improves meaningfully, expand.

**Prerequisite:** §3.1 (need spans) and §3.2 (need a struct layout).

**Expected speedup:** 2×–8× on the vectorized functions.

### 3.6 Retire legacy image-size constants

**Where:** `ImageUtility.kMAX_IMAGE_WIDTH` and
`ImageUtility.kMAX_IMAGE_HEIGHT` in `Graphics/ImageUtility.cs`.

**Why:** `BlobMap` already uses an iterative `Stack<Point>` flood fill and
does not consult these values. The public constants remain only as legacy
compatibility surface for any external caller that reads them.

**Change:** after confirming no external callers depend on them, remove the
two constants:

```csharp
public static int kMAX_IMAGE_WIDTH = 2048;
public static int kMAX_IMAGE_HEIGHT = 2048;
```

**Expected benefit:** a smaller public API with no behavior change to the
red-eye pipeline.

### 3.7 Precompute lookup tables

**Where:** `RgbaHls.RGB_2_CIEXYZ` and any function that applies the sRGB
"gamma" curve (`v <= 0.04045 ? v/12.92 : pow(...)`), and the 24‑bit RGB
integer → normalized float step in `GetCIELABImage`.

**Why:** with only 256 possible `byte` values per channel, `sRGB→linear`
and `linear→sRGB` transforms can be replaced by 256‑entry lookup tables
(or 3 × 256 = 768 entries for the whole RGB→XYZ matrix pre‑applied).
`Math.Pow` is one of the slowest scalar operations on the CPU and is
called *three times per pixel* currently.

**Change:**

```csharp
private static readonly double[] SrgbToLinear = BuildSrgbToLinearTable();
// srgb: byte 0..255 -> linear: double 0..1
```

**Expected speedup:** 2×–3× on `GetCIELABImage` on its own.

### 3.8 Run the pipeline off the UI thread

**Where:** `MainForm.OnLoad` currently runs `ProcessImage` synchronously
before the form is displayed. The window appears frozen for the duration.

**Change:** make the harness async:

```csharp
protected override async void OnLoad(EventArgs e)
{
    base.OnLoad(e);
    var original = RedEye.Properties.Resources.redeye;
    var processed = await Task.Run(() => new RedEyeTool().ProcessImage(original));
    modifiedImage.Image = processed;
}
```

**Expected speedup:** none in wall time, but the UX goes from "app hangs
on launch" to "app appears immediately, result populates when ready." This
matters more for real inputs than the tiny embedded sample.

### 3.9 Replace legacy collections & silent exception handling

- Wherever `Hashtable` / `ArrayList` are used in `ImageUtility` /
  `BlobMap`, switch to `Dictionary<TKey,TValue>` / `List<T>`. Generic
  collections avoid per‑element boxing of value types, which alone can
  cut allocation traffic dramatically in `BlobMap.RemoveIslands`.
- Delete the empty `catch (Exception) { }` in
  `RedEyeTool.ProcessImage`. Silent failure is a correctness bug (the
  design doc flags it as such). At minimum:

  ```csharp
  catch (Exception ex)
  {
      Debug.WriteLine($"RedEye pipeline failed: {ex}");
      throw;   // or return original bitmap, but never swallow
  }
  ```

Not a raw performance change, but it prevents "quiet slowness" — a
partially‑completed pipeline masquerading as a successful one.

### 3.10 (Optional) GPU acceleration

If the pipeline needs to run on very large images or at video framerates,
consider offloading the per‑pixel steps to the GPU:

- **[ComputeSharp]** — write compute shaders in C#, works on Windows via
  DirectX 12. Targets .NET 6+.
- **[ILGPU]** — cross‑platform (CUDA / OpenCL / CPU). More general.

Both are large commitments and only make sense once the CPU path in
§3.1–§3.5 has been exhausted. For single‑crop red‑eye removal, CPU with
SIMD is almost always enough.

---

## 4. Suggested Order of Work

A pragmatic sequencing that keeps the app buildable and correct at every
commit:

| # | Change | Depends on | Rough effort |
| --- | --- | --- | --- |
| 1 | Golden test + `Stopwatch` benchmark (§2.2 Stage 0) | — | 0.5 day |
| 2 | Convert `.csproj` to SDK style, still `net40` (§2.2 Stage 1) | 1 | 0.5 day |
| 3 | Retarget to `net8.0-windows` (§2.2 Stage 2) | 2 | 1 day |
| 4 | Retarget to `net10.0-windows`, `AnyCPU` (§2.2 Stage 3) | 3 | 0.5 day |
| 5 | `CIELab` → `readonly struct` (§3.2) | 4 | 0.5 day |
| 6 | `LockBits` + `Span<byte>` everywhere (§3.1) | 4 | 2–3 days |
| 7 | Run pipeline off UI thread (§3.8) | 6 | 0.25 day |
| 8 | Precompute sRGB LUTs (§3.7) | 6 | 0.5 day |
| 9 | `Parallel.For` on outer loops (§3.4) | 6 | 0.5 day |
| 10 | Fuse intermediates / drop `Bitmap` for masks (§3.3) | 6 | 1–2 days |
| 11 | Retire legacy image-size constants (§3.6) | — | 0.5 day |
| 12 | SIMD hotspots (§3.5) | 5, 6 | 2–4 days |
| 13 | Split into `RedEye.Imaging` library (§2.5) | 4 | 1 day |
| 14 | Optional: ImageSharp / GPU (§2.5, §3.10) | 13 | multi‑day |

Steps 1–4 are the *modernization* backbone; nothing about the algorithm
changes. Steps 5–12 are the *performance* backbone; nothing about the
public API of `RedEyeTool.ProcessImage` needs to change. Steps 13–14 are
architectural and only worth pursuing if RedEye is going to be lifted into
a larger product.

---

## 5. Success Criteria

A change is considered a win only if all of the following hold:

1. **Correctness.** The golden hash of the processed sample image is
   unchanged, or differs only in ways explainable by a documented change
   in floating‑point ordering (e.g., SIMD reduction order). In the latter
   case, add a per‑channel tolerance test (`|Δ| ≤ 1` on 8‑bit output).
2. **Speed.** The `Stopwatch` benchmark on a representative image
   (recommend a 1024×1024 crop, not just the tiny embedded sample) shows a
   non‑trivial improvement — at least 10% for a "small" change, or a
   measurable step‑function for a big one (LockBits, SIMD, parallel).
3. **Simplicity.** The change does not multiply the surface area of
   `unsafe` blocks or introduce new native dependencies without a written
   justification in the commit message.
4. **CI green.** `dotnet build -warnaserror` and `dotnet test` pass on the
   currently‑targeted framework, and on each transitional framework during
   the migration.

---

## 6. What This Document Deliberately Does *Not* Recommend

- **A full rewrite in a new language / runtime.** The algorithm is small,
  well‑understood, and works. Every recommendation above preserves it.
- **Switching UI framework (MAUI / WinUI / WPF / Avalonia).** The harness
  is 20 lines of code; porting the UI is orthogonal to modernizing the
  runtime and offers no algorithmic benefit. If UI portability is needed,
  §2.5 (extract library) is a strictly better investment.
- **Introducing ML‑based red‑eye detection.** That is a *feature*
  extension (as noted in `design.MD` §6), not a modernization or
  performance concern, and belongs in a separate design document.

[.NET Upgrade Assistant]: https://learn.microsoft.com/dotnet/core/porting/upgrade-assistant-overview
[try-convert]: https://github.com/dotnet/try-convert
[SixLabors.ImageSharp]: https://github.com/SixLabors/ImageSharp
[ComputeSharp]: https://github.com/Sergio0694/ComputeSharp
[ILGPU]: https://www.ilgpu.net/
