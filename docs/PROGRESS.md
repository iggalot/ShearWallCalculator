# Progress Log

_Branch `dev/wind-core-refactor` in both repositories. Companion to `ROADMAP.md`, `AUDIT.md` and `DATA_VERIFICATION.md`._

## 2026-10-30 (later) — Fourth look, and the wind equation audit

- **Wind setup:**
  - The ASCE 7 edition is Step 1, with a note on which IBC references which edition.
  - The steps are renumbered 1–6.
  - The tinted page background makes the white cards stand out, and selected cards are light blue.
  - The 0° slope switch waits 0.8 s, down from 1.2 s, so a typo can still be fixed before it flips.
- **Shear walls:**
  - The Calculations tab says what to draw until there is something to calculate.
  - Walls with no load in a direction show "-" instead of NaN, and only the results table for this
    calculator type is shown.
  - Test Design no longer calls `AddLoads(15, 15)`, which had replaced the linked wind loads with a test load
    while the screen still said "ASCE 7 wind".
  - Checked in the app: an 80 × 60 ft box gives 7.50 k per W-E wall (15.0 / 2) and 12.18 k per S-N wall
    (24.4 / 2).
- **`WIND_EQUATION_AUDIT.md`:**
  - The equations check out against ASCE 7 and the published 7-22 change summaries.
  - **F1 (high):** C&C GCp is read at the zone's total area instead of the component's effective wind area,
    which is unconservative.
  - **F2 (high):** the 16 psf C&C minimum (30.2.2) is not applied.
  - **F3:** the Exposure B Kz floor needs to be confirmed.
  - F1 and F2 need your decision before the engine changes.

## 2026-10-30 — Third look: workspaces, stepped site page, detailed calculations

**Shear-wall app**
- A **Wind loads | Shear walls** switch at the top of the window. Wind loads shows the guided setup and
  the wind tabs across the full width. Shear walls shows the drawing canvas, its toolbar, and the
  Dimensions and Calculations tabs. The Dimensions and Calculations tabs had been left out at startup
  since the "UI Improvements / TabControlManager" commit (033f02a); they are back. Next to the switch, a
  line always says which loads the walls use (wind, linked, or MANUAL).
- On the Wind Setup Results page, "Next: Shear walls" opens the Shear walls workspace.
- **LOADS dialog:**
  - Opens on ASCE 7 wind. Choosing Manual asks for confirmation, because it ends the wind link.
  - The old embedded building and wind input forms are replaced by a summary of the current wind loads,
    the cases 1 and 3 option, the load-case descriptions, and "Change the wind inputs…", which goes to
    the Wind loads screen.
  - Fixed: the dialog's old constructor took (magX, locX, magY, locY), so V_y was passed as an X
    location and the manual Y box always opened at 0.

**Wind setup (W7)**
- The Site page has one labelled row per step: Risk Category, Wind speed (highlighted), Exposure,
  Building type (enclosure), Terrain (hill / Kzt). Choosing "On a hill" puts the cursor in the Kzt box.
- Results:
  - The four stat cards share one layout (label / number + unit / two detail lines), so the numbers
    line up.
  - Load cases 1–4 are described in plain words; cases 2 and 4 fade when the exception is chosen.
  - Matching west/south checks are shown once.
- **Detailed calculations window** (button on Results). This is the replacement for the CC Wind Calcs,
  MWFRS 1 and MWFRS 2 tabs, which stay until it is accepted. Its pages:
  - Inputs and factors (α, zg, Kh, Kzt, Ke, Kd, qh with Eq. 26.10-1, GCpi, G, a).
  - Cladding: roof and wall zone drawings; GCp graphs drawn from this building's curves, with every zone
    marked at its effective area; tables grouped by zone and size.
  - Wind from W and wind from S: the force breakdown and minimum, zone drawings, plan and elevations,
    and the roof and wall tables.
  - The Fig. 27.3-8 load cases.
- **Fixed radio groups:**
  - WPF groups radio buttons by name across a window. The two 3D views (setup and 3D tab) unchecked
    each other's mode buttons, so nothing looked selected.
  - The roof-shape and roof-Cp buttons also shared the group "roof".
  - Each control instance now has its own groups.
- The 3D view hides the roof Cp choice when it does not apply.
- **0° slope:**
  - Choosing Gable or Hip with a 0 slope starts at 15°.
  - Typing 0 switches to Flat after a short pause or on leaving the box. Before, the switch needed the
    box to lose keyboard focus, which clicking the 3D view does not do.

Tests: W7 engine 217 passed (2 skipped), W7 UI 42 passed, SWC 23 passed.

## 2026-10-29 (later) — M5 polish after the second look

- **Wind workspace:** on the Wind Setup and 3D Wind tabs the shear-wall drawing canvas and its toolbar are hidden, so the wind screens get the whole window. The canvas returns on every other tab.
- **Shear walls use the wind loads by default.**
  - The walls follow the wind automatically from startup; the Results step shows a green "Shear walls use these wind loads" box.
  - A manual load (LOADS dialog) or opening a file ends the link. The box then turns amber ("Shear walls are NOT using these wind loads", with the reason) and shows a "Use these wind loads for the shear walls" button.
  - The main toolbar shows "Loads: ASCE 7 wind (auto)" or "Loads: MANUAL".
- **Site step:**
  - The wind speed section is highlighted and says which risk-category map to use.
  - "Kzt = 1.00" is always shown for flat ground; Yes makes it editable.
- **Building step:**
  - The inputs column is narrower (390 px) and the 3D view takes the rest.
  - Typing a 0° pitch on a gable or hip switches to Flat when you leave the box.
- **3D viewer:**
  - The toolbar is one compact row (Cladding: Suction / Push-in; Shear walls: From W / From S; Roof Cp).
  - The view buttons and the Blocks/Outlines toggles sit inside the picture.
  - The legend is a slim bar, about a quarter of its old size.
- **Results step:** the checkbox reads "Use load cases 1 and 3 only", with when it is allowed shown underneath.
- **Text:** smaller fonts and shorter wording throughout.
- **Sizing:** the fit test now uses the tab size measured in the running app beside the canvas (1134 × 754), the smallest case.
- **Tests:** engine + UI 249 (+2 opt-in), shear wall 23.

## 2026-10-29 (late) — M5 revisions after the first look

- **No scrolling:** every step fits the Wind Setup tab beside the drawing canvas (about 1130 × 790 on a maximized 1920 × 1080 screen). A UI test checks each step at that size, including a gable roof with Kzt shown and the longest checklist (h > 60 ft).
  - **Site:** compact rows: edition | wind speed | hill, then risk, exposure and enclosure as rows of cards.
  - **Building:** inputs on the left half, the 3D view on the right half, smaller roof-shape buttons.
  - **Results:** the prototype layout, with four headline cards across the top, the 3D view below on the left, and Checks plus Shear walls on the right.
- **Shear walls stay linked to the wind** (fixes the walls keeping the loads from an earlier building):
  - After "Send loads to shear walls" (or choosing ASCE loads in the LOADS dialog), every wind or building change re-sends the load cases automatically, and the Results step says so.
  - If the inputs become uncalculable, the walls get no wind load rather than old loads.
  - A manual load, or opening a file, ends the link.

## 2026-10-29 (night) — M5: guided wind input

Built from the approved HTML mockup. The scope is the C&C and MWFRS methods only; parapets, overhangs
and multi-story input are deferred.

- **New "Wind Setup" tab**, which is the first tab and opens on startup. It has three steps:
  - **Site**:
    - code edition (now defaults to 7-22);
    - risk category as plain-language cards ("selects the map, does not multiply the loads");
    - wind speed in mph, with a link to the ASCE Hazard Tool;
    - exposure picture cards with the upwind-distance rules (B 2,600 ft or 1,500 ft for h ≤ 30 ft; D 5,000 ft plus 600 ft downwind; each or 20h if greater);
    - hill yes/no, revealing Kzt;
    - the four enclosure classes.
  - **Building**:
    - roof-shape cards;
    - L, B and eave height with units;
    - pitch with rise per 12;
    - ridge direction for gable roofs only;
    - the live 3D view, mean roof height, ridge height, and whether the cladding method applies.
  - **Results**:
    - highest roof uplift and wall suction, with the zone;
    - force from the west and from the south (walls + roof, minimum, and which governs);
    - a checklist;
    - the Fig. 27.3-8 cases 1 and 3 exception;
    - **Send loads to shear walls**.
- **Live updates:** every keystroke recalculates the screen. The rest of the app follows after a 0.35 s pause.
  - Invalid input shows its message at the field and clears the results everywhere; it never leaves old numbers showing.
  - The classic BuildingData and WIND Input tabs remain and stay in sync both ways.
- **Narrow window:** the columns stack when the tab is narrow (beside the drawing canvas).
- **Engine (`ASCE7Wind.Core/Guidance`):** `WindInputValidator` (field-level checks tied to the code limits) and `WindSummary` (the headline numbers), both unit tested.
- **Fixed:** the classic WIND Input tab reset risk category to II and exposure to C whenever it was opened, and it dropped G.
- **Tests:** engine + UI 245 (+2 opt-in), shear wall 23.

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
