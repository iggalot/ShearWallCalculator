# Code Audit — ShearWallCalculator + ASCE7-Wind-Load-Calculator

_Audit date: 2026-10-29 · Branch: `audit/roadmap-2026-10` · Baseline: `CC_windcalcs @ 0c64be5`_

No code was changed for this audit. Paths are relative to each repository root:

- **SWC** = `F:\Programming\ShearWallCalculator`
- **W7** = `F:\Programming\ASCE7-Wind-Load-Calculator`

---

## 1. What is in the box

| Project | Type | Target | Role | Status |
|---|---|---|---|---|
| SWC `ShearWallCalculator.csproj` | Library | .NET Fx 4.7.2 | Walls, diaphragms, rigid/flexible distribution, NDS/SDPWS tables, Simpson catalog | Works; has WPF dependency |
| SWC `ShearWallVisualizer` | WPF app | .NET Fx 4.7.2 | Geometry editor (draw walls/diaphragms, snap, zoom, reference image), results tabs, load dialog | Main app |
| SWC `WindLoadDataExporter` | WPF app | .NET Fx 4.7.2 | Batch-exports wind pressures for a size sweep (mostly commented-out code) | Developer tool, stale |
| SWC `ShearWallCalculatorDriver` | Console | **net7.0** | `Main()` is empty | Dead |
| W7 `ASCE7WindLoadCalculator.csproj` | Library **with WPF UI** | .NET Fx 4.7.2 | ASCE 7-16/7-22 C&C + MWFRS calcs, building input, results controls, 2D drawings | The wind engine |

The solution file references W7 by relative path (`..\ASCE7-Wind-Load-Calculator`), so both folders must sit side by side.

### Which wind copy is the real one?

There are two copies of the wind code: the external **W7 repo**, and the tree that lived in SWC up to commit `9831668^` (2025-08-07, "Removed ASCE7 Wind Load Calculator code to its own project").

I compared them file by file, after normalizing the namespace and `using` changes:

- **Calculation code (`WindLoadCalculations/**`, `BuildingData.cs`)** is identical in both copies. The only differences are namespace and `using` lines.
- **UI controls and helpers:** W7 is newer.
  - `WindLoadInputControl` has about 355 changed lines in W7 (auto-recalculate, safer parsing).
  - `BuildingDataInputControl` has about 85 changed lines.
  - `EffectiveWindAreaRenderer` has about 32 changed lines.
- **W7 has one uncommitted edit** in `UserControls/WindLoadsUserControls/WindLoadInputControl.xaml.cs`. It adds null-safe parsing with defaults and a null `BuildingData` guard. It looks like a good change, but it is not committed yet.
- None of the 12 SWC branches contains wind code newer than W7.

**Verdict: W7 (`main @ c67831c` plus its uncommitted edit) is the most recent and most complete copy.** The SWC history copy is a strict subset of it.

---

## 2. What works

- **Geometry editor (SWC `MainWindow`).** Line and rectangle modes, snapping, grid, zoom/pan, reference-image underlay with a measured scale, save/load to JSON, and a recent-files list. It is the most mature part of the app.
- **Rigid and flexible diaphragm distribution.** Center of rigidity and center of mass, direct shear, torsional shear, and result tables.
- **NDS/SDPWS tables 4.2A–C and 4.3A–C, plus the Simpson HDU/HTT/strap catalogs.** They are transcribed and loadable.
- **Wind engine architecture.** It has the right building blocks:
  - polygonal effective-wind-area zones, with holes, area and centroid;
  - a piecewise-curve evaluator;
  - factories per edition, roof type and ridge direction;
  - a separate calculator per analysis type.
- **The C&C zone polygons** (`*AreaCalculator_CC_*`) are the most valuable asset after the curve data. They are also exactly what a 3D view needs, because every zone is already a polygon.
- **Coefficient data is mostly faithful to ASCE 7.** See `DATA_VERIFICATION.md`. Most curves are sampled on the code's log-area scale, so linear interpolation between the 8 samples stays within a few hundredths.

---

## 3. What does not work (defects)

Severity: 🔴 wrong answer or crash · 🟠 wrong in some cases or a silent failure · 🟡 quality issue.

### Engineering / calculation

| # | Sev | Where | Defect |
|---|---|---|---|
| D1 | 🔴 | SWC `ShearWallVisualizer/Dialogs/LoadInputDialog.xaml.cs:113,121` + `.xaml:32` | **Unit error, 1000×.** Wind base shear is summed as psf × ft² = **lb**, but it is passed into `V_x`/`V_y`, which the dialog and calculator treat as **kips**. |
| D2 | 🔴 | same lines | **Internal pressure is double-counted** in base shear. `NetPressure` already includes ±GCpi. Adding windward net to minus leeward net adds 2·qh·GCpi, which should cancel for a closed box. Overestimates the lateral load by roughly 25–35%. |
| D3 | 🔴 | W7 `…/ASCE7_22/CC/Figure30_3_ASCE7_22/Figure30_3_2G_ASCE7_22.cs:22–25` | **Sign typo:** `RoofCurves_Neg["Zone3"]` goes `-1.43 → +1.0` at A ≥ 100 sf. Suction flips to positive pressure, which is unconservative. |
| D4 | 🔴 | W7 `Figure30_3_2A_ASCE7_16.cs:117,128,139,150` and `…2A_ASCE7_22.cs:116,127,138,149` | **Sign typos in the overhang curves.** `(200, 1.6)`, `(200, 1.36)` and `(200, 1.32)` should be negative. Overhang suction between 100 and 500 sf is badly wrong. |
| D5 | 🔴 | W7 `…/Figure27_3_1_*/Chapter27RoofFigureFactory_ASCE7_16.cs` (and the 7-22 copy, l.39–57) | **h/L = 0.5 exactly returns `null`** for normal-to-ridge roofs with θ ≥ 10°. The branches are `≤0.25`, `(0.25,0.5)`, `(0.5,1.0)` and `≥1.0`, so 0.5 matches none of them. That ratio is easy to hit with round numbers (e.g. h = 20, L = 40), and the result is a `NullReferenceException` downstream. |
| D6 | 🟠 | W7 `WallAreaCalculator_MWFRS_ASCE7_16.cs:44` and `…_22.cs:45` | **Gable-end height is wrong when the ridge is parallel to the length.** It uses `length/2` where it should use `width/2`, which oversizes the gable triangle on the windward and leeward walls. `BuildingData.ComputeRidgeHeight()` has it right, so the duplicated geometry has drifted. |
| D7 | 🟠 | W7 `ASCE7_22/WindLoadCalculator_ASCE7_22_Base.cs:54–72` | **7-22 uses the 7-16 exposure constants** (`2.01`, zg = 1200/900/700, α = 7/9.5/11.5). ASCE 7-22 Table 26.10-1 revised these. *Verify against the printed table.* |
| D8 | 🟠 | W7 `ASCE7_*_Base.cs` `GetKz` | The C&C rule for Exposure B (Kz taken at z ≥ 30 ft, i.e. 0.70, per Table 30.3-1) is not applied. Kz floors at 15 ft (0.575), which **underestimates C&C pressure by about 18%** for low Exposure B buildings. *Verify the edition note.* |
| D9 | 🟠 | W7 `WindLoadCalculator_MWFRS_ASCE7_*.cs:22` | **Leeward Cp interpolates straight from L/B = 1 to 4.** The table has a point at L/B = 2 (-0.3). The code gives -0.4 there: conservative, but off by up to 0.1. |
| D10 | 🟠 | W7 `WindLoadCalculator_MWFRS_*.cs:70,97` and `WindParameters_Base.cs:13–14` | `G = 0.85` is hard-coded, so the `GustFactor` input is ignored. `ImportanceFactor` multiplies qz, but there has been no wind importance factor since ASCE 7-10. Both inputs are misleading. |
| D11 | 🟠 | W7 MWFRS calc | Every wall uses qh. Windward qz(z) is never used: conservative, but the `MWFRS_WW_0/15/h` zones suggest it was intended. Load Cases 2–4 (torsion, Fig. 27.3-8), the 16/8 psf minimums (27.1.5) and the horizontal roof projection in base shear are also missing. |
| D12 | 🟠 | W7 `CC/WindLoadCalculator_CC_*.cs:19–34` | **The h > 60 ft limit only writes to `Console`** and leaves the curves `null`. The user sees a blank panel or a crash later, with no message. |
| D13 | 🟠 | W7 `BuildingInfo/BuildingData.cs:55` | `ValidateRidgeDirection()` runs only in the constructor, before the object-initializer values are applied. Invalid roof-type/ridge combinations survive. |
| D14 | 🟠 | SWC `ShearWallCalculator_RigidDiaphragm.cs:82–87` | `ecc = Math.Max(min, actual)` throws away the sign: an actual -10 ft becomes +min. The 5% minimum is a **seismic** accidental-torsion rule. Wind torsion should come from Fig. 27.3-8 load cases. |
| D15 | 🟠 | SWC `ShearWallCalculator_RigidDiaphragm.cs:117,138,147` | `DisplayResults()` has the `ContainsKey(id) is false` test inverted, then indexes `[id]`, which throws `KeyNotFoundException` whenever it runs. |

### UI / graphics

| # | Sev | Where | Defect |
|---|---|---|---|
| U1 | 🟠 | W7 `WindLoadGraphicCanvas.xaml.cs:119–245` | Pressures in psf are used directly as **pixel** offsets and arrow lengths. Labels print raw doubles such as `17.3412398`. |
| U2 | 🟠 | same, l.165 | Sidewall arrow x-position uses `0.25*(e.X+f.X)`, a sum, where it should interpolate `f.X+0.25*(e.X-f.X)`. Arrows land off the wall. |
| U3 | 🟡 | same | Pressures are looked up by **display string** (`"Windward Wall - z=h"`), so renaming a label breaks the drawing. |
| U4 | 🟡 | W7 all controls | Every redraw clears and rebuilds hundreds of `Shape` elements on a `Canvas`. Nothing scales to the viewport, and there is no hit-testing to link a zone to its row in the table. |
| U5 | 🟡 | SWC `LoadInputDialog` | The dialog rebuilds the whole `WindLoadInputControl` whenever building data changes, so in-progress input is lost. |
| U6 | 🟡 | everywhere | Errors go to `Console.WriteLine`, which a WPF app does not show. Users get no feedback. |

---

## 4. Redundancy

| Item | Size | Notes |
|---|---|---|
| **ASCE 7-16 vs 7-22 MWFRS area calculators** | ~1,750 lines | Flat, gable and hip, parallel and perpendicular, and the `_Base` class: **byte-identical apart from class names.** Zone geometry does not depend on the edition. |
| 7-16 vs 7-22 C&C flat roof, walls, `Figure30_3_1`, `Figure30_3_2A`, bases | ~600 lines | 0–10 differing lines each. Only the coefficient data differs. |
| `DrawingHelpers.cs` (1,341 lines) | ×2 | Identical copies in SWC `ShearWallVisualizer/Helpers` and W7 `Helpers`. |
| 7× `TryGetGCp_*_ByAreaID` | ~250 lines | One generic `TryGetCoefficient(zones, curves, x)` replaces all seven. |
| 3× `CalculateWallPressures_*` / `CalculateRoofPressures` per calculator | ~100 lines each | Identical loops over different dictionaries. |
| 13 `Dictionary<int,PressureData>` properties | — | They should be a single `List<ZoneResult>` tagged by surface, direction and sign. |
| `BuildingData.EnclosureType` **and** `WindParameters.EnclosureClassification` | — | Two sources of truth, and only the first is used (`GetGCpi`). |
| Ridge-height and gable geometry | 3 places | `BuildingData`, `WallAreaCalculator_MWFRS_*` and `BuildingDrawer` each compute it, and they already disagree (D6). |
| `ShearWallCalculatorBase.BuildingHeight` vs `BuildingData.BuildingHeight` | — | The code itself marks this with a TODO ("need to make this sync"). |

## 5. Unneeded / dead

- `ShearWallCalculatorDriver`: empty `Main()`, and it targets net7.0 against net472 libraries.
- `FlatRoofWindLoadParameters`, `GableRoofWindLoadParameters`, `HipRoofWindLoadParameters`, and `WindLoadParametersFactory` that picks between them: all three subclasses are empty.
- `WindParameters_Base.GCp_Values`, `RiskCategory` (a string, used for nothing), `ImportanceFactor` and `GustFactor` (see D10).
- The unused locals `bldg_length_wall_param` and `bldg_width_wall_param` in `WindLoadCalculator_Base.CreateAreaCalculators`.
- `WindLoadDataExporter`: about 60% commented-out code. Replace it with a unit-test and verification harness (see the roadmap).
- `reference_docs/AI_shear_wall_calculator_`: an untyped scratch C# snippet.
- `TODO.txt`: partly done. Fold it into the roadmap.
- The misnamed folder `ASCE7_22/MWFRS/Figure27_3_1_ASCE7_16/`, which holds the 7-22 classes.
- Repeated `[JsonIgnore]` on private fields, and `Console.WriteLine` diagnostics in library code.

## 6. Structural issues (the root causes)

1. **The calculation libraries depend on WPF.** `System.Windows.Point` and `Rect` appear in `EffectiveWindArea`, `AreaCalculator_Base` and `DiaphragmData_Rectangular`, and the SWC core references `PresentationCore`/`WindowsBase`. That blocks headless unit tests, any other front end, and modern .NET.
2. **The wind library mixes engine and UI.** W7 contains both the ASCE math and the WPF controls, so it cannot be reused without dragging WPF along. That works against your original goal of modules reusable across projects.
3. **Inheritance is organized by edition** (`*_ASCE7_16` / `*_ASCE7_22` class pairs) when the editions differ only in **data**: coefficient tables, Kz constants, and where Kd is applied. That is why ~50% of the wind code is duplicated.
4. **Logic keys off magic strings.** Zone labels (`"ZoneWW"`, `"Zone2e"`) link areas to curves to results to drawings, and a typo fails silently.
5. **There are no automated tests.** The only verification is `VerificationSpreadsheet.xlsx`, so the D3/D4 sign typos went unnoticed.
6. **The project format is old-style csproj on .NET Framework 4.7.2,** with no NuGet `PackageReference` and no CI.
7. **Wind and shear walls are not really connected.** The only link is two numbers, V_x and V_y, passed through a dialog (with D1 and D2 in that path). Floor levels, which diaphragm gets which load, and the wind load cases are all missing.
