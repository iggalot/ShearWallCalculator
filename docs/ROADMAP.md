# Roadmap — From Calculator to Interactive 3D Wind & Shear-Wall Tool

_2026-10-29 · Audience for the finished app: non-engineers · Priorities: correctness → ease of use → performance_

Companion docs: `AUDIT.md` (defects D#, U#), `DATA_VERIFICATION.md` (data errors V#).

## Destination

You open the app, answer about 6 plain-language questions (where the building is, size, roof shape), and see a **3D model of your building that you can rotate with the mouse**. The roof and walls are colored by wind zone and pressure. Hovering a zone shows *"Roof corner — 42 psf uplift"*. One click switches between cladding pressures (C&C) and whole-building loads (MWFRS), or changes the wind direction. The same building feeds the shear-wall editor, which receives correct base shears in kips for every wind direction and load case.

## Target architecture

```
                 ┌────────────────────────────────────┐
                 │  BuildingModel  (single source)    │  footprint, eave h, roof type/pitch/ridge,
                 │  pure C#, no WPF                   │  enclosure; derives h, ridge, faces
                 └──────────────┬─────────────────────┘
                                │ Surfaces (3D faces + local 2D frames)
          ┌─────────────────────┼─────────────────────┐
          ▼                     ▼                     ▼
┌──────────────────┐  ┌──────────────────────┐  ┌───────────────────────┐
│ Asce7.Wind.Core  │  │ Asce7.Wind.Core      │  │ ShearWall.Core        │
│ Zoning (polygons)│→ │ Pressures            │→ │ base shear / story    │
│ edition-agnostic │  │ edition = DATA table │  │ forces → wall shears  │
└──────────────────┘  └──────────────────────┘  └───────────────────────┘
          │                     │                         │
          └──────────┬──────────┴─────────────────────────┘
                     ▼  WindResult { Zone, Surface, Polygon3D, GCp, p+, p- }
        ┌───────────────────────────────┐       ┌───────────────────────┐
        │ Asce7.Wind.Wpf (controls)     │       │ ShearWallVisualizer   │
        │  • 3D viewer  • input wizard  │  ←──  │  (host app, editor)   │
        │  • results table/charts       │       └───────────────────────┘
        └───────────────────────────────┘
```

Key rules:

- **Core libraries contain no WPF types.** They use their own `Vec2`, `Vec3` and `Polygon2` types.
- **ASCE editions are data, not class hierarchies.** One calculator takes an `IAsce7Edition` table: coefficients, Kz constants and Kd placement.
- **Zones are an enum or record, not strings.** For example `Zone(Surface.Roof, Id.Z2e)`.
- **One result type** feeds the table, the 3D colors, the charts and the shear-wall link.

## Managing the two repositories

The two repos stay separate:

- **W7** (`ASCE7-Wind-Load-Calculator`) owns everything ASCE 7: core, WPF controls, verification tests. It must not reference SWC.
- **SWC** owns the shear-wall core and the host app. It consumes W7 via the existing sibling `ProjectReference`.

The workflow:

- Each piece of work uses the **same branch name in both repos**, for example `audit/roadmap-2026-10` or `m1-verification`. Commits stay in the repo they touch, and no commit spans both.
- SWC keeps `docs/W7_COMPAT.md`, recording the W7 commit hash that each SWC milestone was built and tested against. That makes it possible to check out a matching pair later.
- Every milestone starts from a **clean, committed tree in both repos**.
- Submodules or a local NuGet feed are possible later. They are not needed now, and the sibling layout already works.

---

## Milestones

Each milestone ends with a buildable app, a commit in each touched repo, and green tests.

### M0 — Baseline & safety net *(½ day)*
- Resolve the uncommitted W7 edit to `WindLoadInputControl.xaml.cs` (commit it as a "WIP baseline", or discard it; your call).
- Create the matching branches in both repos and write `W7_COMPAT.md`.
- Confirm that the solution builds in your Visual Studio with nothing extra installed.

### M1 — Correctness & verification harness *(2–3 days)*
- Add a test project to W7. Option A: MSTest, already present with VS, no download. Option B: xUnit, which needs a NuGet restore and your approval.
- Turn the curve-consistency check into a test. Add golden-value tests for qz, Kz, GCpi and Cp at known points, and a handful of full-building cases taken from your `VerificationSpreadsheet.xlsx`.
- Fix **V1–V5**, then **D1–D6, D9, D12, D13**. Confirm **D7/D8** against the printed code before changing them.
- Exit criterion: tests reproduce your spreadsheet within ±1%.

### M2 — Wind engine extraction *(4–6 days)*
- Split W7 into `Asce7.Wind.Core` (no WPF) and `Asce7.Wind.Wpf`.
- Collapse the 7-16/7-22 class pairs into **one calculator plus two edition data tables**. This removes about 4,000 lines.
- Replace the 13 pressure dictionaries and 7 `TryGetGCp_*` methods with `IReadOnlyList<ZoneResult>` and one lookup.
- Store curves as code anchor points and interpolate on log₁₀(A).
- Turn errors into a `ValidationResult` list ("Mean roof height 64 ft exceeds the 60 ft limit for this method"), not `Console` output.
- The existing WPF controls switch to the new API, so behavior is unchanged and the tests stay green.

### M3 — Unified 3D building model *(3–4 days)*
- Add `BuildingModel` → `IReadOnlyList<Surface>`: walls, roof planes, gable triangles and hip triangles. Each surface carries a 3D outline and a **local 2D frame** (origin, u, v).
- Zone polygons are already computed in a local 2D frame per surface. Mapping them into 3D is just `origin + x·u + y·v`.
- This single model replaces the three ridge-height computations (D6) and the hard-coded 2D drawers.
- Tests check that the surface areas sum to the expected envelope and that zone areas sum to their surface area.

### M4 — Interactive 3D viewer *(5–7 days)*
- **Technology:** WPF's built-in `Viewport3D`. It needs no download and handles well over 10k triangles. HelixToolkit would add a trackball camera and labels out of the box, but it is a NuGet download, so I'd only add it if you approve.
- **Interaction:**
  - Left-drag orbits.
  - Right-drag or middle-drag pans.
  - The wheel zooms toward the cursor.
  - Double-click a zone to focus it.
  - Preset buttons: Iso, Plan, N/E/S/W.
- **Layers:**
  - zones colored by pressure on one sequential scale, with a legend in psf;
  - optional zone outlines and labels;
  - wind-direction arrow;
  - ground grid with a scale;
  - transparency toggle to see the walls behind the roof.
- **Modes:** C&C (pick positive or negative, plus effective area) and MWFRS (pick direction and load case).
- **Linking:** hovering a zone highlights its table row, and selecting a row highlights the zone in the model.
- **Performance:**
  - Build one `MeshGeometry3D` per color bucket, not per zone.
  - Freeze brushes and geometry.
  - Rebuild only when the model changes; camera moves never trigger a recalculation.
  - Debounce recalculation by 150 ms while typing.
  - Target: 60 fps orbit, and under 50 ms from an edit to the redraw.

### M5 — Non-engineer UX *(4–5 days)*
- A **3-step wizard**: Site → Building → Results. The 3D preview updates live from step 2 onward.
- Plain-language labels, with the engineering term in a tooltip. Examples:
  - "Ground roughness around the site" (Exposure B/C/D), with pictures.
  - "How open is the building?" (enclosure).
- Defaults for everything, and inline validation in the field instead of message boxes. Units are always shown, including a "kips = 1,000 lb" hint.
- Edition selector (7-16/7-22) defaults to 7-22, with a one-line "what changed" note.
- Results open on a **summary card** first (maximum uplift, maximum wall pressure, base shear N-S and E-W). Detailed tables are one click away.

### M6 — Connect wind → shear walls *(4–6 days)*
- `WindToLateral` service: for each direction (0°, 90°, 180°, 270°) and load case 1–4, compute the base shear, the eccentricity (Fig. 27.3-8) and the story or diaphragm forces.
- Include the horizontal roof projection and the 16/8 psf minimums. Handle internal pressure correctly (it cancels).
- Feed these into `ShearWallCalculator_Rigid/Flexible` with explicit units (a `Force` type or a `kips` suffix), replacing the hand-entered V_x/V_y. Manual override stays available.
- Fix D14 (signed eccentricity, wind versus seismic rules) and D15.
- Use the shared `BuildingModel`: the shear-wall editor footprint *is* the wind footprint. One definition, no re-typing.

### M7 — Reporting & polish *(3 days)*
- PDF/print report: inputs, assumptions, 3D snapshots per load case, tables and code references.
- Save/load all project data (geometry plus wind inputs) in one versioned JSON file.
- Remove dead projects (Driver, Exporter) or replace them with the test project.

### M8 — Optional modernization
- SDK-style csproj files. Consider .NET 8 (WPF works on `net8.0-windows`), which needs the .NET 8 SDK already on the machine; I will not download it.
- Benefits: faster startup and modern C#. Not required for anything above.

---

## Parking lot (future features)

### P1 — Automatic wind speed from the site address (blocked: needs an ASCE Hazard Tool API key)
- **Source:** the ASCE Hazard Tool API is the official one. It covers the 50 states and territories, needs an API key, and pricing is to be confirmed with ASCE. Do **not** use ZIP or area codes: they are too coarse near coasts and in special wind regions, and the ASCE maps are copyrighted, so a bundled copy is not an option. ATC Hazards by Location was scheduled to shut down at the end of 2024 and never supported 7-22.
- **Flow:**
  1. The user enters the site address.
  2. The app geocodes it to latitude/longitude. The free U.S. Census geocoder is a candidate; confirm before adding any outside service.
  3. It calls the ASCE Hazard Tool API with the user's own key, stored locally only.
  4. It fills in V for the chosen risk category and edition.
- **Record keeping:**
  - Save the value with the project, with its source and date ("ASCE Hazard Tool, 7-22, Risk Cat. II, retrieved YYYY-MM-DD").
  - Keep manual entry as an override, and keep working offline from the saved value.
  - Warn on special wind regions or missing data; the tool has been reported to return "NaN" for some locations.
- **Fit:** this becomes the "Site" step of the M5 guided input. Risk category then selects which wind map V comes from, replacing the old importance-factor input.

### P2 — Roof-to-wall ties (hurricane ties / H-clips)
- **Why:** the eave and overhang uplift is now calculated (item 3b): MWFRS §27.3.3 and the C&C overhang zones. The last step is to size the tie at each rafter or truss. That connection fails often in real storms.
- **Needs:** roof-tie allowable loads (uplift and lateral, by species), similar to the hold-down data. Simpson H-series or the equivalent. Not in the repository yet.
- **Calculation:** uplift per tie = (net roof uplift over the tributary width + overhang uplift) × spacing, less 0.6 D. Check at ASD against the tie allowable, the same way as hold-downs.
- **App:** a roof-tie line on the Design tab, and a row in the report.

---

## Top 20 improvements (priority order)

1. **Fix the 1000× unit error** in the wind → shear-wall handoff: lb versus kips (D1).
2. **Stop double-counting internal pressure** in base shear (D2).
3. **Fix the five coefficient data typos**, including three sign flips (V1–V5).
4. **Fix the h/L = 0.5 crash** in the Chapter 27 roof factory (D5).
5. **Fix the gable-end wall height** when the ridge is parallel to the length (D6).
6. **Confirm and correct Kz** for ASCE 7-22 constants and the C&C Exposure B rule (D7/D8).
7. **Add an automated verification test suite** seeded from your spreadsheet. This prevents regressions like V1–V4.
8. **Show errors to the user** instead of `Console.WriteLine`, e.g. h > 60 ft, invalid inputs (D12, U6).
9. **Remove the misleading inputs** (Importance factor, Gust factor, Risk Category string) or wire them in correctly (D10).
10. **Make ASCE editions data, not class pairs.** This deletes about 4,000 duplicated lines (§4 of the audit).
11. **Split W7 into Core (no WPF) and WPF**, so the engine is truly reusable across projects.
12. **Replace magic zone strings with typed zones,** and the 13 pressure dictionaries with one result list.
13. **Create one `BuildingModel`** as the single source of geometry for wind, drawing and shear walls.
14. **Build the rotatable 3D viewer** with pressure-colored zones, a legend, hover info and view presets.
15. **Add a two-way link** between 3D zones and the results table, with highlight on hover or select.
16. **Add a guided 3-step wizard** with plain-language labels, defaults and inline validation, for non-engineers.
17. **Compute MWFRS for all directions and load cases** (including torsion, Fig. 27.3-8) and feed them automatically to the shear-wall calculator.
18. **Correct the rigid-diaphragm eccentricity logic,** keeping the sign and separating wind from seismic rules (D14), and fix the `DisplayResults` crash (D15).
19. **Redraw efficiently:** debounced recalculation, frozen geometry, one mesh per color bucket, no full control rebuilds (U4, U5).
20. **Clean the repository:** delete the empty Driver, the stale Exporter, the scratch snippet and the duplicated `DrawingHelpers.cs`. Rename the misnamed folders, add `W7_COMPAT.md` and a README.
