# RedEye — Design Document

## 1. Purpose:

( currently being used to test doc generation with AI )
( adding comment to test )

**RedEye** is a small C# / .NET Windows Forms application that demonstrates an
automated **red‑eye removal** algorithm for digital photographs. The visible
program is a *test harness*: it loads a single embedded sample image
(`redeye.png`), runs the red‑eye removal pipeline against it, and shows the
"before" image alongside the "after" image so the result can be inspected
visually.

The bulk of the codebase is **not** UI — it is a self‑contained image
processing library (`Graphics/`) that implements:

- Color‑space conversions between RGB, HSL, CIE XYZ and CIE L\*a\*b\*
- Image utilities (convolution, Gaussian blur, thresholding, color matrices,
  resizing, etc.)
- Connected‑component ("blob") analysis on binary images
- The red‑eye detection / correction algorithm itself

The harness is therefore most useful as a reference implementation that can be
lifted into a larger photo‑editing application.

---

## 2. Top‑Level Architecture

```
+---------------------------------------------------------+
|                       Program.cs                        |
|         (STA entry point, runs Application loop)        |
+---------------------------------------------------------+
                           |
                           v
+---------------------------------------------------------+
|                       MainForm                          |
|  - sourceImage   PictureBox  (original sample)          |
|  - modifiedImage PictureBox  (algorithm output)         |
|  - label3        Description of the algorithm           |
|  OnLoad:                                                |
|     bitmap = Resources.redeye                           |
|     bitmap = new RedEyeTool().ProcessImage(bitmap)      |
|     modifiedImage.Image = bitmap                        |
+---------------------------------------------------------+
                           |
                           v
+---------------------------------------------------------+
|                    Graphics layer                       |
|                                                         |
|   RedEyeTool   ── orchestrates the pipeline             |
|   ImageUtility ── pixel ops, convolution, BlobMap       |
|   RgbaHls      ── RGB / HSL / CIE conversions           |
|   XColorMatrix ── 5x5 color‑matrix helper               |
+---------------------------------------------------------+
```

### Project layout

| Path | Role |
| --- | --- |
| `Program.cs` | Standard WinForms `Main` — enables visual styles and runs `MainForm`. |
| `MainForm.cs` / `MainForm.Designer.cs` / `MainForm.resx` | UI: two `PictureBox` controls and a static description label. |
| `Properties/Resources.resx` (+ `.Designer.cs`) | Embeds `redeye.png` as the demo input. |
| `Properties/AssemblyInfo.cs`, `Properties/Settings.*` | Standard generated project metadata. |
| `Graphics/RedEyeTool.cs` | The red‑eye removal pipeline (`ProcessImage`). |
| `Graphics/ImageUtility.cs` | Static helpers for bitmaps, plus the `BlobMap` connected‑components class. |
| `Graphics/RgbaHls.cs` | Color and color‑space math (RGB ⇄ HSL, RGB ⇄ CIE XYZ ⇄ CIE L\*a\*b\*, chromaticity distance). |
| `Graphics/XColorMatrix.cs` | Convenience wrapper over `System.Drawing.Imaging.ColorMatrix`. |
| `RedEye.csproj` / `RedEye.sln` | MSBuild / Visual Studio project files. |
| `app.config` | Targets .NET Framework 4.0. |

### Build / runtime target

- **Language / framework:** C# on **.NET Framework 4.0**
- **UI toolkit:** `System.Windows.Forms` + `System.Drawing` (GDI+)
- **Platform target:** `x86` (some unsafe blocks are enabled in `Debug|x86`)
- **Output type:** `WinExe` (assembly name `RedEye.exe`)

There are no external NuGet dependencies — the project only references the
standard BCL assemblies (`System`, `System.Drawing`, `System.Windows.Forms`,
`System.Data`, `System.Xml`, etc.).

---

## 3. The Red‑Eye Removal Algorithm

The algorithm lives in `Graphics/RedEyeTool.cs`. The public surface is a single
method:

```csharp
public Bitmap ProcessImage(Bitmap b);
```

It takes an RGB image that is assumed to be a *cropped region around the eye*
(the algorithm does not perform eye detection on a full‑frame photo — that step
must be done upstream) and returns a new bitmap in which the red pupil has been
neutralised to gray while keeping highlights and lighting intact.

### 3.1 Why CIE L\*a\*b\* ?

Red‑eye is fundamentally a **chromatic** problem (an unnaturally saturated red
color) and not a brightness problem. CIE L\*a\*b\* separates lightness (`L*`)
from the two opponent‑color channels (`a*` = green↔red, `b*` = blue↔yellow).
Working in this space lets the algorithm:

1. Measure "how red" each pixel is using only `a*` and `b*` (chromaticity).
2. Replace the color of red‑eye pixels while keeping their `L*` value, so
   *specular highlights and shading are preserved*.

A canonical "typical red‑eye" reference color is hard‑coded:

```csharp
const double kL_REDEYE = 42;
const double kA_REDEYE = 55;
const double kB_REDEYE = 30;
```

### 3.2 Pipeline steps

`ProcessImage` chains a series of intermediate bitmaps. Each step is summarised
below, with a pointer to the function that implements it.

| # | Step | Implementation |
| --- | --- | --- |
| 1 | Convert RGB → CIE L\*a\*b\* per pixel; record min/max `L*`. | `GetCIELABImage` |
| 2 | Compute a grayscale **redness mask** based on chromaticity distance from the reference red‑eye color. White = closest to red, black = farthest. | `GetMaskImage` → `GetChromaticityDistanceMap` |
| 3 | Threshold the redness mask at `175/255` to produce a binary candidate map. | `Threshold` (uses `ImageUtility.ThresholdImage`) |
| 4 | Gaussian‑blur the binary map and re‑threshold to clean up noise / merge adjacent specks. | `Threshold` (second pass) |
| 5 | Run connected‑component analysis on the binary map. | `BlobMap` constructor (`Graphics/ImageUtility.cs`) |
| 6 | Drop blobs that don't meet the size criteria (small noise islands are removed). | `BlobMap.RemoveIslands` |
| 7 | Fill interior holes in the remaining blobs by inverting, blob‑mapping again, filling, and re‑inverting. | `FillHoles` + `BlobMap.FillHoles` |
| 8 | Gaussian‑blur the final binary mask so the correction blends smoothly into surrounding pixels. | `ImageUtility.Convolve` with `GetGaussianBlurKernel(3, 15)` |
| 9 | Build a grayscale target image in CIE L\*a\*b\* (set `a*=0`, `b*=0`, stretch `L*` to the full dynamic range), then blend the original and target per‑pixel using the blurred mask as weight. | `GetNewImage` |

Step 9 is the mathematical core of the correction. For each pixel:

```
L =  Lₜ · m + L_orig · (1 − m)
a =  0  · m + a_orig · (1 − m)
b =  0  · m + b_orig · (1 − m)
```

where `m ∈ [0, 1]` is the mask weight at that pixel and `Lₜ` is the
dynamic‑range‑stretched lightness value:

```
Lₜ = (maxL / (maxL − minL)) · (L_orig − minL)
```

Pixels with `m ≈ 1` (inside the detected pupil) become a luminance‑preserving
gray, while pixels with `m ≈ 0` (outside the mask) are left untouched. The
Gaussian‑blurred mask edges give a soft transition that hides the boundary of
the correction.

A `ColorIslands` debug step (`falseColor`) is also computed but is not part of
the final output — it exists so a developer can visualize the blob map.

### 3.3 Why this design

- **Locality of changes.** Only pixels that are both *spatially clustered* and
  *chromatically close to red‑eye red* are touched. This avoids recoloring
  natural skin tones or red clothing in the same crop.
- **Luminance preservation.** Because the correction works in CIE L\*a\*b\*
  and only desaturates (sets `a*` and `b*` toward 0), the eye keeps its
  highlights and shape, producing a far more natural result than naïvely
  painting the pupil black.
- **Soft mask blending.** Gaussian blur on the binary mask, combined with the
  linear interpolation in step 9, prevents visible seams around the pupil.

---

## 4. Supporting Components

### 4.1 `ImageUtility` (`Graphics/ImageUtility.cs`)

A large grab‑bag of static helpers used by the red‑eye pipeline (and useful in
their own right). Highlights:

- `CreateImage(w, h, pf [, fillColor])` — allocates a bitmap, optionally
  filled with a color.
- `ThresholdImage(b, t)` — in‑place binary threshold on a normalized `[0,1]`
  intensity.
- `Convolve(b, kernel, kernelSize)` — generic 2D convolution.
- `GetGaussianBlurKernel(size, sigma)` — builds a normalised Gaussian kernel
  for use with `Convolve`.
- `BlobMap` (class) — connected‑components analysis on a binary image. It
  records, for each pixel, the index of the island it belongs to (`‑1` for
  background), and maintains a list of islands with their seed location, pixel
  count, and edge‑connectedness. Used by the red‑eye pipeline for noise
  removal (`RemoveIslands`), false‑coloring (`ColorIslands`), and hole filling
  (`FillHoles`). Hard limits of `2048 × 2048` are documented because some
  internal algorithms use stack arrays sized to those constants.

### 4.2 `RgbaHls` (`Graphics/RgbaHls.cs`)

An object representing a color in synchronised RGB‑A and HSL representations,
plus a comprehensive set of *static* color‑space conversion methods:

- `RGB ⇄ HSL`
- `RGB ⇄ CIE XYZ`
- `CIE XYZ ⇄ CIE L*a*b*`
- `ChromaticityDistance(a1, a2, b1, b2)` — Euclidean distance between two
  points in the `a*b*` plane, which is what `RedEyeTool` uses to score
  "redness".

`RedEyeTool` uses these static helpers; it does not instantiate `RgbaHls`
objects.

### 4.3 `XColorMatrix` (`Graphics/XColorMatrix.cs`)

A thin wrapper around `System.Drawing.Imaging.ColorMatrix` for building and
composing 5×5 color transformation matrices (translation, scale, rotation in
color space, etc.). Not used by the red‑eye pipeline itself, but ships with
the library because it pairs naturally with the convolution / threshold
utilities for other image effects.

---

## 5. UI Behavior

The form (`MainForm`) is intentionally trivial — it exists to make the
algorithm visible.

- Two `PictureBox` controls show the sample image *before* and *after*
  correction, side by side.
- A static label below describes the algorithm in plain English (the same
  description as Section 3 above, in shorter form).
- All work happens in `OnLoad`:

```csharp
protected override void OnLoad(EventArgs e)
{
    Bitmap b = RedEye.Properties.Resources.redeye;
    RedEyeTool ret = new RedEyeTool();
    b = ret.ProcessImage(b);
    this.modifiedImage.Image = b;
}
```

There are no controls for opening other files, saving the result, or tuning
parameters — those are left as integration points for a hosting application.

---

## 6. Known Constraints & Extension Points

The current code is a demonstration, and the following limitations are worth
calling out for anyone planning to productize it:

- **No eye detection.** The input is assumed to already be a tight crop around
  the eye. A real product needs face/eye detection (e.g. via OpenCV / Windows
  ML / a learned model) upstream of `ProcessImage`.
- **Per‑pixel `GetPixel` / `SetPixel`.** Several routines (notably the CIE
  conversions and `GetNewImage`) use `Bitmap.GetPixel` / `SetPixel`, which is
  extremely slow on large images. Migrating to `LockBits` + pointer access (or
  `Span<byte>`) is the obvious first optimization.
- **Hard‑coded thresholds.** The threshold (`175`), Gaussian parameters
  (`size=3, sigma=15`), and reference red‑eye color
  (`L*=42, a*=55, b*=30`) are constants in `RedEyeTool.cs`. Exposing them as
  parameters would let callers tune for skin tone, lighting, or different
  flash conditions.
- **Silent failure on exceptions.** `ProcessImage` wraps the whole pipeline in
  `try { … } catch (Exception) { }` and returns whatever `newImage` is at the
  point of failure (potentially `null`). Real usage should surface or log
  errors.
- **Image size limit.** `BlobMap`'s recursive flood‑fill style algorithms cap
  out at `2048 × 2048`. Larger inputs need either an iterative implementation
  or down‑sampling.
- **.NET Framework 4.0 / WinForms only.** Porting to .NET 6+ and either WPF /
  WinUI / MAUI or a headless library (no `System.Drawing.Common` on
  non‑Windows) is straightforward but required for cross‑platform use.

---

## 7. Quick Reference

- **Entry point:** `Program.Main()` in `Program.cs`
- **Algorithm entry point:** `RedEyeTool.ProcessImage(Bitmap)` in
  `Graphics/RedEyeTool.cs`
- **Color math:** `RgbaHls.*` (static) in `Graphics/RgbaHls.cs`
- **Pixel ops & blob analysis:** `ImageUtility` and `BlobMap` in
  `Graphics/ImageUtility.cs`
- **Demo image:** `redeye.png`, embedded via `Properties/Resources.resx`
- **Build:** open `RedEye.sln` in Visual Studio (or run `msbuild RedEye.sln`)
  targeting .NET Framework 4.0, platform `x86`.
