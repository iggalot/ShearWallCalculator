# Progress Log

_Branch `dev/wind-core-refactor` in both repositories. Companion to `ROADMAP.md`, `AUDIT.md` and `DATA_VERIFICATION.md`._

## 2026-10-29 (evening) — M6: wind loads into the shear walls

- **Directional force** (`WindLateralLoads`, ASCE7Wind.Core), for each wind direction:
  - walls: windward integrated with qz up the height, plus leeward;
  - **plus the horizontal roof component** (F = p·Zx·A_plan on each sloped plane), for both Fig. 27.3-1 roof Cp values, with the larger total governing;
  - not less than the **27.1.5 minimum**: 16 psf on the projected wall area and 8 psf on the projected roof area.
  - The roof component was previously **missing**. On a 30° gable with wind normal to the ridge it is about 46% of the total.
- **Fig. 27.3-8 load cases** 1–4 (16 sub-cases including ± torsion, e = 0.15B):
  - The exception (cases 1 and 3 only) is a checkbox in the load dialog: one story with h ≤ 30 ft, or two stories or less light-frame or with flexible diaphragms.
- **Shear walls**:
  - `ShearWallCalculatorBase.LoadCases`: the rigid and flexible calculators analyse every case, and each wall reports its governing case (shown as a tooltip on the results row).
  - Wind cases carry their own torsion and do not add the manual 5% minimum eccentricity.
  - Manual loads behave exactly as before.
- **Load dialog**: the ASCE path sends all load cases and shows walls + roof, the minimum, and what governs.
- **3D view**: the summary shows "Wind force on the building: total (walls + roof; 16/8 psf minimum)".
- **Pre-existing bugs found and fixed**:
  - **D23:** the flexible diaphragm's Y-direction distribution mixed plan axes (doubled the load in the test box).
  - **D24:** the Calculations tab results rows never displayed (inverted guard).
- **Tests:** engine 196 (+2 opt-in), UI 19, shear wall 23.

**Needs your sign-off:**
- The Fig. 27.3-8 exception wording.
- Using DesignKips = max(calculated, 27.1.5 minimum) inside every load case. That is conservative; the code treats the minimum as a separate load case.
- Applying wind loads at the diaphragm center of mass. That approximates the center of the projected area for regular plans.

## 2026-10-29 (later) — M3 and M4 complete

### M3 — One building model in 3D (`ASCE7Wind.Core`, no WPF)
- `BuildingModel` builds the envelope from `BuildingData`:
  - four walls, with gable triangles on the gable ends;
  - the roof planes for flat, gable (either ridge direction) and hip roofs;
  - outward normals, wall local frames, and the roof plan regions with their plane equations.
- `WindSceneBuilder` places every C&C and MWFRS zone on the envelope:
  - Roof zones are drawn in plan. They are triangulated, clipped to each roof plane and lifted onto
    it, so a zone that crosses a ridge or hip folds correctly.
  - Wall zones map through each wall's frame.
  - The width-direction MWFRS results (which use the building turned 90°) are mapped back onto the
    real faces.
- `PolygonOps` provides triangulation that is robust to holes touching the edge (the flat-roof edge
  zone has these), plus convex clipping.
- `PressureColorScale` is a diverging red/blue scale with gray at zero. `OrbitCamera` handles orbit,
  pan, zoom, framing and named views.
- 29 tests:
  - Envelope areas, gable and hip geometry, outward normals.
  - Triangulation and clipping.
  - Zones cover the true sloped roof area and lie on the roof planes, for every roof type, edition and
    view.
  - MWFRS windward/leeward placement.
  - Color ordering and camera behavior.

### M4 — Interactive 3D viewer (`Wind3DViewerControl`, WPF `Viewport3D`, no packages)
- **Views:**
  - cladding suction;
  - cladding inward pressure;
  - whole building with wind from the west, or from the south;
  - both Fig. 27.3-1 roof cases.
- **Mouse:** drag to orbit, right-drag to pan, wheel to zoom, double-click to reset. Preset views:
  3D, Top, South, West.
- **Hover:** shows the pressure, a plain-language zone name, the surface, the zone code and the
  effective area, and highlights the zone.
- **On-screen information:**
  - legend in psf: blue pushes on the surface, red pulls away;
  - summary card: highest uplift and wall suction, or windward/leeward walls plus base shear in kips;
  - limit warnings;
  - labeled wind and north arrows.
- **In the host app:** a new **3D Wind** tab, refreshed whenever the wind inputs change, plus
  **View → 3D Wind View (window)** for a full-size resizable window.
- **Tests:** 12 offscreen rendering tests in `ASCE7Wind.UI.Tests` cover flat, gable (both ridge
  directions) and hip roofs in every view, hover picking, warnings and invalid input.

**Test totals:** W7 171 (+2 opt-in), UI 12, ShearWallCalculator 18. Both solutions build with 0 errors.

### Not done yet (next candidates)
- Two-way linking between the 3D view and the results tables. The viewer already raises
  `ZoneHovered` and offers `ZoneAt()`; the tables need a matching selection hook.
- Load cases 2–4 (torsion, Fig. 27.3-8), and roof horizontal components in the base shear (M6).
- Typed zone identifiers in place of strings (M2 remainder).
- The guided input wizard (M5).

## 2026-10-29 — M0 complete, M1 complete, M2 mostly complete

### M0 — Baseline
- The uncommitted W7 edit (null-safe parsing in `WindLoadInputControl`) is committed as the baseline (`7870687`).
- Both repos are on `dev/wind-core-refactor`, and both solutions build with 0 errors.
- MSTest 4.0.2 is restored offline from the local package cache (no downloads).

### M2 — Engine extraction and de-duplication

Behavior was proven unchanged at each step with a **golden master**: 3,168 building, edition and analysis
combinations, every zone's area, centroid and pressure, compared byte for byte.

- `ASCE7Wind.Core`: a new WPF-free engine project. It uses its own `Vec2`/`Bounds2` types, and the UI
  converts at the drawing boundary.
- ASCE 7-16 and 7-22 differences are now data (`Asce7Edition`) instead of paired classes.
  This removed about 3,700 lines.
- One lookup and one pressure loop replace the seven `TryGetGCp_*` methods and the duplicated loops.
- **Performance** (Release, calculation-only benchmark, same 3,168 buildings):

  | | Before | After |
  |---|---|---|
  | Full analysis | 60–76 µs | ~10 µs |
  | UI-style result reads | 70–91 µs | 14–16 µs |

### M1 — Verification against the code, and fixes

**Tests:**
- W7 has 142 tests, organized by ASCE 7 chapter: 26 (Kz, qz, Kd, GCpi), 27 (MWFRS Cp, Eq. 27.3-1,
  zones), 30 (C&C figures, edge strip *a*, zoning, figure selection, curve-integrity sweep, limits),
  geometry, and base shear.
- ShearWallCalculator has 18 tests: rigidity, center of rigidity and mass, direct and torsional shear
  equilibrium, and minimum eccentricity.

Fixed (IDs refer to `AUDIT.md` / `DATA_VERIFICATION.md`):

| ID | Fix |
|---|---|
| D1, D2 | Wind base shear passed to the shear-wall calculator is in **kips**, external pressure only (internal pressure cancels) |
| D5 | h/L = 0.5 exactly no longer returns "no figure" |
| D6 | MWFRS gable-end wall rise uses the width when the ridge runs along the length |
| D7 | ASCE 7-22 Kz uses the 7-22 terrain constants (2.41; zg 3280/2460/1935; α 7.5/9.8/11.5) |
| D9 | Leeward wall Cp honors the L/B = 2 table point |
| D10 | G comes from the inputs; the importance factor no longer multiplies qz and is removed from the screen |
| D12 | h > 60 ft (C&C) and slopes > 45° show a plain-language message on the results screen instead of crashing |
| D13 | Ridge direction is resolved from the inputs, whatever order properties are set in |
| D14 | Rigid-diaphragm eccentricity keeps its sign |
| D15 | Rigid-diaphragm results table no longer throws |
| D16 | Roofs at exactly 7°, and hip roofs ≤ 7°, use Fig. 30.3-2A zoning and coefficients consistently |
| D17–D20 | Wall C&C zoning: each wall uses its own length; gable ends follow the ridge; the 7-22 gable apex is at mid-span; a wall exactly 2a long is all Zone 5 |
| D21 | `ShearWallCalculatorBase(walls, diaphragms, Vx, Vy)` stored its arguments into themselves |
| — | MWFRS leeward roof Cp applies to both load cases; MWFRS hip end faces now receive coefficients (they were silently dropped) |
| — | ASCE 7-22 hip roofs 27–45° use Fig. 30.3-2G (the blend between 2F and 2G is removed) |
| — | Chapter 30 curves interpolate on log(area), as the figures are drawn |
| V1–V5 | Five coefficient data-entry errors corrected |

## Needs your engineering confirmation

These were implemented to the best reading of the code, or deliberately left alone. Please check each
against your printed standard.

1. **ASCE 7-22 Kz constants (D7).** The constants reproduce the tabulated 15 ft values (0.57 / 0.85 /
   1.03) and the 7-16 C/D values at 60 ft. Exposure B at 60 ft becomes 0.83 (it was 0.85). Confirm
   against 7-22 Table 26.11-1.
2. **C&C Exposure B below 30 ft (D8).** Not changed. Kz still floors at 15 ft. Confirm whether your
   edition requires z ≥ 30 ft for C&C in Exposure B.
3. **Fig. 30.3-2A Zone 2 (V4).** The value is changed −2.5 → −2.3 for the roof. The matching overhang
   curve still starts at −2.5 and was not changed.
4. **7-16 Fig. 30.3-2E overhang, Zone 1.** In the low h/B set, |GCp| *increases* with area
   (−1.8 → −2.0). This is listed as a known review item in the integrity test; the data is unchanged.
5. **7-16 Fig. 30.3-2H/2I.** Curves come from slope equations with evenly indexed intermediate points,
   and the Amin/Amax limits are still TODO. Compare them with the figure.
6. **7-22 hip roofs 27–45°.** These now use Fig. 30.3-2G as a step range. Confirm that the 2F/2G blend
   was not intentional.
7. **MWFRS hip roofs.** Hip end faces use the normal-to-ridge windward/leeward Cp at the roof slope.
   Below 10° they use the Zone 4 values, which is conservative. Fig. 27.3-1 has no hip-specific table.
8. **7-16 C&C gable-end walls.** These are zoned as rectangles below the eave, which is the existing
   behavior; 7-22 includes the gable triangle. Decide whether 7-16 should include it too.
9. **Rigid-diaphragm minimum eccentricity** (5% of the plan dimension or 10% of building height). The
   sign is now kept, but the minimum rule is unchanged. That rule is a seismic accidental-torsion
   concept; wind torsion comes from the Fig. 27.3-8 load cases (roadmap M6).
10. **Base shear scope.** Base shear uses wall pressures only. Horizontal roof components, the 16 psf
    minimum and the torsional load cases come with M6.

## Next

- **M2 remainder:** typed zone identifiers in place of `"Zone2e"` strings, and one result list in place
  of 13 dictionaries. This needs coordinated UI changes.
- **M3:** a single `BuildingModel` with 3D surfaces and local frames (the zones are already per-surface
  polygons).
- **M4:** the `Viewport3D` viewer with mouse orbit, pan and zoom, and pressure-colored zones.
