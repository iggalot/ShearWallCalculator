# Progress Log

_Branch `dev/wind-core-refactor` in both repositories. Companion to `ROADMAP.md`, `AUDIT.md` and `DATA_VERIFICATION.md`._

## 2026-10-31 (late night) — Sheathing on one or two faces, any SDPWS table (SWC)

- **Each wall face picks its own sheathing** from SDPWS Table 4.3A (wood structural panels), 4.3B (wood over gypsum) or 4.3C (gypsum wallboard, lath, plaster, fiberboard), wind columns. Design tab columns: Face 1 / Nails / Face 2 / Nails. Face 2 is None, "Same as face 1", or any other row. Table 4.3A/B faces pick an edge spacing (only the spacings the row has); Table 4.3C rows carry their own fastener spacing. The species factor applies to the wood tables only.
- **Two-face rule** (user): same material on both faces, 2 × one face; different materials, the larger of the stronger face and 2 × the weaker; for wind, wood structural panel or structural fiberboard + gypsum wallboard are added. Settings bar: "Add wood + gypsum faces (wind)", on for new projects, saved with the project.
- **Aspect ratio per face** (Table 4.3.4): a face over its limit does not count, with a note; wood panels take 1.25 − 0.125 h/b above 2:1, fiberboard 1.09 − 0.09 h/b above 1:1.
- **Defaults:** details card buttons "Use this sheathing for new walls" (saved with the project; walls drawn afterwards start with it) and "Same sheathing on every wall" (asks first; dead loads and hold-down types stay).
- The details list each face, the two-face rule and the capacity. The report table has a "Faces" column (1, 2 × one, sum, stronger, 2 × weaker) and states the project's summing setting. The selection card shows the sheathing and the two-face rule.
- Older projects open unchanged: "2 sides" becomes the same face on both sides.
- Data items to check (repeated 4.3C row, plywood siding rows in 4.3B, gypsum aspect limit, fiberboard factor, gypsum lath values): `DATA_VERIFICATION.md`.
- **Tests:** SWC 107 (15 new, by hand: 4.3C values, gypsum one/two faces, wood + gypsum summed and not, gypsum + plaster not summed, tall wall with gypsum face dropped, species factor, fiberboard aspect factor, Table 4.3B, option list, older project, deep clone, auto-select, project file round trip, summing on by default).

## 2026-10-31 (night) — Drawing tools: typed points, object snaps, select and details card (SWC)

- **Typed points** (`Drawing/CoordinateInput.cs`): the Point box on the status line, or just start typing a number while drawing. `12,30` absolute; `@5,0` relative; `@20<90` length and angle (degrees from +x); `20` a length toward the cursor. Feet-inches everywhere: `12'6"`, `12'-6"`, `6"`. A wall end that is not E-W or N-S is squared, and the status line says so; a bad entry says why.
- **Object snaps** (`Drawing/Snapping.cs`, like OSNAP): endpoint □, midpoint △, intersection ×, perpendicular ⊥ (from the first point), nearest ⧗, on walls and diaphragm edges. Ranked in that order within 10 px; the glyph and its name show at the snapped point, the preview line follows it. On by default; F3 or S toggles; ▾ next to Snap picks which. The old snap reach ignored the zoom; it is now in screen pixels.
- **Select tool** (Esc, or the toolbar): click a wall (within 6 px) or the smallest diaphragm under the cursor. A card over the drawing shows the size and position; for a wall also the design shear and its case, sheathing, nailing, unit shear / capacity, hold-down and T, pass/fail, with Design (opens the Design tab on that wall), Edit (Walls & diaphragms tab) and Delete (asks first). The tables follow the selection. Esc: cancels a shape, then closes the card, then switches to Select.
- Status line: length of the wall being drawn, or the rectangle size; the active snap.
- **Tests:** SWC 92 (15 new: each snap kind and its rank, perpendicular needs a start, parallel/non-crossing segments, typed absolute/relative/polar/length, feet-inches, error messages, picking wall over diaphragm and the inner diaphragm).

## 2026-10-31 (evening) — Logging, error handling, legacy pages restyled (SWC)

- **Log:** `%AppData%\ShearWallCalculator\logs\swc-yyyy-MM-dd.log`, kept 14 days; Help > Open log folder. An unexpected error is logged with its detail and reported in a message; the action is cancelled and the work stays open. Warnings and errors also appear on the right of the status line (click opens the folder). Tests write their log to the temp folder.
- **Toolbar:** one row in the workspace-bar style: DRAW Wall (L) / Diaphragm (R), Snap (S), SHOW Grid / Image / Walls & diaphragms, Loads... and the load summary. The active tool is lit. Removed: Test Design (debug), the calculator class name, the Snap and Debug message boxes.
- **Status line:** what the tool expects next ("Wall: click the second end (right-click cancels)"), cursor in ft, zoom, latest warning. Esc cancels a shape.
- **Fixed:** the L/R/S/D/Z shortcuts fired while typing in any box (a "d" in a wind input toggled debug mode with a message box). They now act only in the Shear walls workspace and never on text input. Moving the mouse or zooming recalculated everything and rebuilt every panel on each move; it now only redraws.
- **Walls & diaphragms tab** (was Dimensions): typed coordinates. A wall keeps its direction (start x, start y, length, height; the end follows); a diaphragm is its lower-left corner, width and depth. Clicking a row highlights the shape on the drawing; × deletes.
- **Calculations tab:** summary strip (loads, center of mass, center of rigidity, Σk, J or wall lines), then "All load cases (design shear)" — most +/− per wall with their cases and the design V — or any single case: rigid shows x̄, ȳ, direct, torsion, total and the case's e and Mt,comb; flexible shows the wall lines (tributary, w, share, K, arm, torsion) and each wall's direct/torsion/total. Flexible warnings are shown.
- Removed the six legacy controls these replace. **Tests:** SWC 77 (log: daily file, levels, exception detail, event, purge, unwritable folder).

## 2026-10-31 (later) — Company default list shipped with the app; version record in the project

- **Company default:** `ShearWallVisualizer/HoldownStandardList.default.json` ships with the app, with a version number. Each user's list follows it automatically unless the user has changed their own; then they are asked once per new version. The catalog window has a "Company" column, **Restore company default** and **Save as company default...** (writes the next version). New projects start on the standard list when it has options. The full procedure is in `PROPOSAL_STANDARD_HARDWARE_AND_FLEX_TORSION.md`.
- **Project record:** on save, the project stores the list version and each wall's hold-down. On opening, any wall whose hold-down would now differ is listed with both versions. The report states the list version.
- **Shipped file is empty (version 0)** until the company list is chosen.
- **Tests:** SWC 71 (startup cases: seeded, updated, asks once, keep-mine remembered, pre-version file, missing file; shipped file loads with known keys; project record round trip and differences).

## 2026-10-31 — Company standard hold-downs, flexible-diaphragm torsion (SWC; design in `PROPOSAL_STANDARD_HARDWARE_AND_FLEX_TORSION.md`)

- **Company standard hold-down list** (user: belongs to the app, "company minimum standard hardware"). Catalog options get stable keys `Model#n` (n-th fastener option of the model). The list is one app file, `%AppData%\ShearWallCalculator\HoldownStandardList.json`, loaded at startup and saved on every change. Edit it with the new "Standard" check boxes in View > Hold-down catalog (filter "Only the standard list"; keys no longer in the catalog are listed under the table, kept, never selected).
- **Design tab:** "Hold-downs: Full catalog / Standard list (n)" in the settings bar. Changing it, or ticking a box in the catalog, re-runs every wall at once. The choice is saved with the project (`DesignSettings.HoldownsFromStandardList`). With the standard list, a wall with no adequate standard option gets no hold-down and says so ("no standard-list hold-down strong enough"); it never quietly falls back to the full catalog. The report states the pool and lists the standard keys. The selected hold-down text now names the minimum post, e.g. "HDU14-SDS2.5 (36 1/4 SDS, min. 3.5 × 5.5)".
- **Flexible diaphragm torsion** (replaces "Mt not applied"): equivalent weights per WoodWorks. K = tributary share (Σ K = 1 per direction), center = Σ K·location, J = Σ K d². E-W lines +K·Mt·ȳ/J, N-S lines −K·Mt·x̄/J, shared by rigidity on each line and added to the direct shear. Checks on the sheet: Σ torsional = 0 each way, moment about the center = −Mt. Stated assumption: per-direction normalization of K. Fewer than two lines in a direction → Mt not applied, with a note. Hand-calculation test: 40 × 20 plan, lines x = 0/10/40 and y = 0/20, Mt = 50 k-ft → x_c = 20, y_c = 10, J = 350 ft², line forces +0.357/+0.714/−1.071 and ∓0.714 k.
- **Chapter 28 zones:** user answers recorded in `DATA_VERIFICATION.md` (zones on the walls by case; Case B values confirmed). Still assumed: Case B torsional reduces only 5T/6T.
- **Tests:** SWC 62 (standard-list selection, the "none fits" message, unique keys, file round trip, project round trip; three flexible-torsion tests).

## 2026-10-30 (late) — Statics review, calculation sheets, all wind directions, hold-down catalog (SWC f0a85d5, a4aa4fb; W7 47bd5de)

- **Sign convention (both apps):** building axes x right, y up; moments CCW positive, M = (r × F)z = rx·Fy − ry·Fx. Wall shears are the force each wall exerts on the diaphragm (opposite to the load).
- **Rigid diaphragm:** Mt_comb = Vx·ecc_y − Vy·ecc_x + Mt (ecc = CR − CM) confirmed. The torsional shear had the opposite sense to the direct shear (its moment about the CR came out +Mt_comb instead of −Mt_comb; diaphragm not in equilibrium, residual 2·Mt_comb). Now +k·Mt_comb·ȳ/J (E-W) and −k·Mt_comb·x̄/J (N-S). Hand example (40 × 20 ft, stiff west wall, Vy = 10 k): walls −7.505 / −2.495 k, E-W couple ±5.009 k; moments about the west wall close to zero.
- **Flexible diaphragm (reviewed from scratch):** tributary method confirmed for uniform load (simple spans; outer lines take the overhang as a force only). Fixed: wall shears now carry the same sign convention as the rigid calculation; fewer than two wall lines in a direction warns instead of throwing. **Open (resolved 2026-10-31, see above):** load cases with a torsional moment Mt are flagged but Mt is not applied (how Mt becomes a non-uniform diaphragm load is undecided; the 27.3-8 and 28.3.2.1 exceptions exempt one- and two-story flexible-diaphragm buildings from the torsional cases).
- **Wind from every side:** Chapter 27 cases now include the 180° directions (Case 1 ±, Case 2 ± wind × ± torsion, Cases 3 and 4 in all four diagonals): 32 cases (8 with the 1-and-3 exception). Chapter 28: each building corner as the reference corner for both wind axes — (Vx, Vy, M), (Vx, −Vy, −M), (−Vx, −Vy, M), (−Vx, Vy, −M) — basic and torsional, plus the minimum ±: 20 cases (12 exempt).
- **Chapter 28 per-zone vector model:** each zone (split at the plan center where it becomes a T zone) has signed p, true area, outward normal, centroid, r from the CM, F = −p·A·n and Mz. Case B side walls 1/4 reported (they cancel in ΣFy); unequal roof halves 2/3 give a cross-wind Vy on gables. Details page: sign convention, zone tally, totals, cases in calculation and building axes.
- **Per-wall range:** most positive and most negative shear with their cases; design V = larger magnitude; hold-downs in pairs.
- **Report:** rigid and flexible calculation sheets (formulas, wall properties, every case with its subcalculations and statics checks).
- **Hold-down catalog (View menu):** every model and fastener option from `NDSProvisions/SimpsonCatalog`, read through `ShearWallDesigner.CatalogRows()` (the same managers the selection uses; a test checks they match), with the walls using each option. To check: HDU14-SDS2.5 lists 36 ¼ SDS on 3.5 × 5.5 twice (10770 and 14445 lb; likely 5.5 × 5.5 for the second).

## 2026-10-30 (night) — Item 3c: Chapter 28 envelope procedure (W7 bc95ffd)

- **Choice:** Results step, Shear walls section: "Ch. 27" (default) or "Ch. 28 low-rise". Stored in `WindParameters_Base.LateralProcedure` and in project files. The shear-wall link, summary, details and report all follow it.
- **Method (Fig. 28.3-1, 7-16 and 7-22):** p = q_h·K_d·(GC_pf); internal pressure cancels horizontally.
  - Wind normal to a gable ridge: Case A at the roof angle (interpolated 5–20–30/45–90°), walls 1/4 and roof 2/3 horizontal components.
  - Along the ridge or flat roof: Case B walls 5/6 (same values as Case A at 0° for the walls), gable triangle included.
  - End zones 2a at the reference corner; a = min(0.1·least, 0.4h) ≥ max(0.04·least, 3 ft).
  - Negative Zone 2 only up to min(0.5·depth, 2.5·h_e), rest of the windward slope Zone 3.
  - Roof neglected if it lowers the shear ("not less than with the roof neglected").
  - Parapets per §28.3.2 (same as §27.3.4).
- **Load cases sent to the walls (per direction):** basic (±torsion: end zones at either corner), torsional (zones beyond the center at 25%, ±), and the §28.3.4 minimum (16/8 psf) as its own case. The torsional cases are skipped with the existing exemption checkbox (one story h ≤ 30 ft, or ≤ 2 stories light-frame or flexible diaphragms).
- **Not covered → falls back to Ch. 27 with a note:** h > 60 ft, h > least plan dimension, hip roofs, open buildings.
- **Hand check (7-22, 115 mph, C, 60×40×15 flat):** q_h·K_d = 24.5 psf; from W: walls 456·q = 11.17 k, torsion ±16.5 kip-ft, torsional case 7.37 k / ±54.5 kip-ft, minimum 9.6 k; from S 16.24 k. Chapter 27 gives 15.0 / 24.4 k. App matches.
- **Details:** new "Shear walls: Chapter 28" page (zone plan, zone table, totals, Ch. 27 comparison); load-cases page follows the procedure.
- **To verify against the printed standard:** see DATA_VERIFICATION.md, Chapter 28.
- **Tests:** W7 engine 258 (2 skipped), W7 UI 54, SWC 45.

## 2026-10-30 (late) — Parapet diagrams; C&C parapets (Section 30.6)

- **Shear-wall pages (MWFRS):** a parapet diagram (§27.3.4). It shows +1.5 push on the windward parapet and −1.0 suction on the leeward parapet, with pressures in psf, the parapet height, and q_p at the parapet top. Commit W7 d4501f8.
- **Cladding page (C&C, §30.6, Fig. 30.6-1):** load cases A and B at corners and edges.
  - Each face: p = q_p·K_d·[(GC_p) − (±GC_pi)], with GC_pi applied to each face separately as you confirmed, and the 16 psf minimum.
  - Net on the parapet = q_p·K_d·(GC_p,front − GC_p,back); the internal pressure cancels.
  - Zones confirmed by you: wall Zone 5 / roof Zone 3 at corners, Zone 4 / roof Zone 2 elsewhere.
  - A Fig. 30.6-1-style diagram of the corner cases, plus a table of all four.
  - Wind app only; the shear walls use the §27.3.4 force.
  - Commit W7 1d845dd.
- **Hand check (7-22, 115 mph, Exp C, 3 ft parapet, enclosed, 10 sq ft):**
  - q_p = 29.91 psf.
  - Case A corner: front +30.0, back −63.0, net 83.9 psf (with note 5 below; −85.9 / 106.8 before it).
  - Case B corner: front −40.2, back +30.0, net 61.0 psf.
  - Edge nets: 83.9 and 53.4 psf.
  - All match the app.
- **To verify:**
  - (resolved) GC_pi: see the parapet-envelope entry below.
- **Fig. 30.3-2A note 5 (applied, wording confirmed by the user):** parapet ≥ 3 ft (0.9 m) around a roof with θ ≤ 7° → negative GCp in roof Zone 3 = Zone 2; positive GCp in roof Zones 2 and 3 = wall Zones 4 and 5. Applies to the roof table and to parapet case A. Below 3 ft nothing changes. Same note in 7-16 and 7-22.
- **Parapet GC_pi (§30.6, wording confirmed by the user):** GC_pi from Table 26.13-1 for the porosity of the parapet envelope, a separate Building-page setting (default Enclosed ±0.18; Open or solid = 0; saved in project files). Each face is evaluated under +GC_pi and −GC_pi and both are tabled; the diagram shows the larger. Net load unchanged (internal pressure cancels). Shear-wall loads unaffected.
- **Tests:** W7 engine 246 (2 skipped), W7 UI 52, SWC 45.

## 2026-10-30 (late) — Item 3d: parapets; scope line

- **Parapets (§27.3.4, your wording):** p_p = q_p·K_d·(GC_pn). GC_pn = +1.5 for the windward parapet and −1.0 for the leeward one, with q_p taken at the top of the parapet. Both push downwind, so the force is (1.5 + 1.0)·q_p·K_d·h_p·B.
  - It is added to each directional total, so it reaches the shear walls automatically.
  - The parapet area counts as wall area for the §27.1.5 16 psf minimum.
  - Flat roofs only: a sloped roof shows a message and ignores the parapet.
  - A Parapet section on the Building page appears for flat roofs, and a parapet row on each shear-wall page.
  - The parapet height is saved in project files.
- **Hand check:** ASCE 7-16, 115 mph, Exp C, 60×40×15 flat roof, 3 ft parapet → q_p = 25.39 psf, 7.62 k with the wind along the length.
- **In the app (7-22):** X goes 15.0 → 22.6 k and Y goes 24.4 → 35.8 k, the same as by hand.
- **Not included:**
  - C&C parapet pressures (§30.8).
  - Parapets on sloped roofs.
  - The extra overturning from the parapet force acting above the wall top. The walls use V·h with h = wall height, which slightly understates T when there is a parapet. This is noted for the reviewer.
- **Scope line (agreed in discussion):**
  - The shear-wall app takes lateral load cases → wall forces → wall design.
  - Wind-only detail (cladding, overhangs, framing spacing) stays in W7.
  - Roof ties stay parked (ROADMAP P2).
  - Roof uplift at hold-downs: enter net dead load (D − MWFRS uplift) in the Design tab's Dead (plf) box. Not automatic.
- **Commits:** W7 47f2bf8.
- **Tests:** W7 engine 238 (2 skipped), W7 UI 52, SWC 45.

## 2026-10-30 (late) — Item 3b: roof overhangs

- **Eave overhang** field (ft) on the Building page, in the Size row. It is saved in project files; older files read it as 0.
- **MWFRS (§27.3.3, wording confirmed by you):**
  - The soffit gets Cp = 0.8 upward (qh·G·Cp, no internal pressure), combined with the top-surface Cp at the windward eave.
  - Each whole-building page shows the soffit pressure, the top-surface pressure, the net pressure and the uplift per foot of eave.
  - Example: 7-22, 115 mph, Exp C, flat 60×40×15, 2 ft overhang → +16.7 psf soffit, −18.7 psf top, −35.4 psf net, 71 lb/ft.
  - This is roof uplift only; the shear-wall forces are unchanged.
- **C&C:**
  - The Cladding page has an overhang table: zone, GCp, source, design pressure, and uplift per foot of eave.
  - 7-16 uses the figure overhang curves. 7-22 uses the 2A curves for slopes up to 7°, otherwise roof + wall GCp (to verify; see `DATA_VERIFICATION.md`).
  - No internal pressure; 16 psf minimum.
- **Roof ties:** added as roadmap item P2.
- **Commit:** W7 b9dd065.
- **Tests:**
  - W7 engine 233 (9 new overhang hand calculations; 2 skipped)
  - W7 UI 52
  - SWC 45 (the project-file round trip includes the overhang)
- **Checked in the app:** the field, the Cladding overhang table, and the §27.3.3 block.

## 2026-10-30 (late) — Item 2: shear-wall design, rigid then flexible diaphragms

- **Flexible diaphragms (second commit):** the same checks use the flexible calculator's wall shears: each wall line takes its tributary load, and the line's walls share it by rigidity. Test: a 40 × 20 ft box, hand-calculated (45 tests). Smoke test: the same 80 × 60 project. With half the N-S wind on one 12 ft wall, that wall fails even with 2" nails on both sides, which is the correct answer.

- **Design tab** (Shear walls mode, after Calculations). Each wall in the list has these inputs:
  - sheathing (SDPWS Table 4.3A, blocked wood structural panels)
  - edge nail spacing
  - sides
  - framing species
  - dead load
  - hold-down type (HDU or HTT to the foundation; a strap to the wall below)
- **Results for each wall:**
  - unit shear v
  - allowed shear
  - ratio
  - hold-down tension
  - the lightest Simpson hold-down that works
  - pass or fail
- Clicking a wall shows every step, with the code reference, in the card below the table.
- In the drawing, walls are green when they pass and red when they fail.
- **Method:**
  - ASD (0.6W, nominal / 2.0) or LRFD (1.0W, 0.80 × nominal).
  - Wood code: SDPWS 2021 or 2015 (see `docs/SDPWS_EDITION.md`).
  - Aspect ratio: h/b ≤ 3.5, with 1.25 − 0.125 h/b above 2:1.
  - Species factor; footnote 10 (×0.92) for 10d nails with the hold-down on the inside face.
  - Hold-downs are always checked at ASD: T = 0.6·V·h/L − 0.6·D·L/2, against the Simpson allowable loads.
    LRFD T is shown for reference.
- **Auto-select nailing (all walls):** picks the lightest passing edge spacing (6", 4", 3", 2"), on one side and then on two sides. The panel and hold-down type stay as they are.
- The design choices are saved with the project.
- The printed report gets a "Shear wall design" section: a summary table and every step for each wall.
- Fixed the hold-down sizing in `ShearWallSelector`: it used to compare the shear V with the hold-down capacity. It now uses the tension T.
- **Tests:** SWC 44 (10 design hand calculations).
- **Smoke test in the app:** an 80 × 60 project linked to wind (140 mph, Exp C).
  - Before auto-select, 0 of 5 walls pass.
  - After auto-select, 4 of 5 pass.
  - The remaining 2.5 ft wall fails correctly: h/b 3.60 > 3.5.
  - The table fits the side panel with no horizontal scroll.

## 2026-10-30 (late) — Item 1: project files and the printable report (M7)

- **One project file (`*.swc.json`, format 2)** holds the wind inputs (edition, site, building, the cladding
  component choices as typed, the cases-1-and-3 option), the shear-wall drawing and load cases, and whether
  the walls follow the wind loads. `Interfaces/ProjectFile.cs`.
- **File menu:** New project (Ctrl+N), Open (Ctrl+O), Save (Ctrl+S), Save As, Open Recent (project files now;
  it used to open nothing), Print report (Ctrl+P). The title bar shows the project name, with `*` for unsaved
  changes. Closing or opening another project asks to save.
- **Opening** restores Wind Setup exactly and re-links the loads. If the wind calculation now gives different
  loads than when saved (more than 0.5%), a message says so with the before/after values. **Older drawing
  files** (no wind inputs) still open with their saved loads, and a note says so. Saving one makes a project file.
- **Printable report:** a cover, every Detailed calculations page, then the shear walls (load cases, wall
  shears and unit shears, governing case). Landscape Letter, with a header and "Page N of M" on every page.
  It is built by the same code as the screen, so the two cannot differ. The preview window prints, and
  "Microsoft Print to PDF" saves a PDF.
- **SDPWS edition:** see `docs/SDPWS_EDITION.md` (tables from 2015; the wind column equals the 2021 v_n).
- Tests: SWC 34 (6 project file, 5 SDPWS); W7 UI 52 (7 report, 1 component restore). Smoke-tested in the app:
  set V = 142, the title shows `*`, Ctrl+S saves, the app closes without a prompt, reopening and Ctrl+O
  brings back V = 142, and Ctrl+P opens the report preview.

## 2026-10-30 (night) — Live results on the Building page; legacy input tabs retired

- The Building page shows the four result cards under the 3D view and updates them on every edit (roof
  component, sizes, slope), so you no longer have to go to Results to see the effect.
- The BuildingData and WIND Input tabs are retired. Wind Setup has every input they had. Their L/B, h/L and h/B
  ratios now appear on the Detailed calculations Inputs page. Their TabItems and controls remain in the code.
- The Wind loads workspace now shows Wind Setup, Detailed calculations and 3D Wind.

## 2026-10-30 (evening) — Audit fixes and the Detailed calculations tab

- **C&C effective wind area (audit F1):**
  - GCp is now read at the component's effective wind area in every zone.
  - The Building page has a "Cladding component" choice for the roof and for the walls.
  - The Results cards say which component and area they are for.
  - The default, 10 sq ft, gives the highest values. Example building: roof Zone 1 suction went from −25 to
    −43 psf, and the headline roof uplift from −73 to −82 psf.
- **16 psf C&C minimum (audit F2):** applied in both directions, and noted in the tables.
- **Exposure B Kz (audit F3):** closed; the code matches the engineer's reading.
- **Detailed calculations tab:**
  - A new tab in the Wind loads workspace that updates on every change; the Results button still opens it in
    a window.
  - The CC Wind Calcs, MWFRS 1 and MWFRS 2 tabs are off the tab strip. Their TabItems and the old controls are
    still in the code, in case they are wanted back.
- **Next:** shear-wall design milestone, waiting on the engineer's Simpson and NDS projects.
- Tests: W7 engine 226 passed (2 skipped), W7 UI 44 passed, SWC 23 passed.

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
