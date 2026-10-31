# Proposal: drawing tools, sheathing types, clean-up, image import, and multi-story walls

_Branch `dev/wind-core-refactor`. Written 2026-10-31 in answer to the user's list. Nothing here is built yet._

## What already exists (scan)

| Item | Status |
|---|---|
| Sheathing tables | **Mostly there.** `NDSProvisions/Table4_3A.cs` (wood structural panels: Structural I, sheathing/single floor, plywood siding, particleboard, fiberboard), `Table4_3B.cs` (wood panels over gypsum), `Table4_3C.cs` (gypsum wallboard, gypsum sheathing, gypsum lath and plaster, cement plaster over metal lath). 9 materials are in `SheathingMaterials`. The design uses only Table 4.3A, and only for Structural I and sheathing grade. |
| Snapping | Endpoints only (wall ends and diaphragm corners), with an on/off button that pops up a message box. |
| Typed coordinates | None. Every point comes from a mouse click. |
| Click to inspect | Wall and diaphragm data panels exist (legacy), but no "click an item → its design" link. |
| Image import | `ImageLayer`, `LayerManager`, `ImagePreviewControl`, `ImageMeasurementWindow`: open a PNG and scale it by hand. |
| W7 3D viewer | Blocks/Outlines check boxes; pressures show only on hover. |
| Errors and logging | `Console.WriteLine` and message boxes; no log file, no unhandled-exception handler. |

## 1. W7: pressure labels on the 3D model (small)
- A **"PSF labels"** check box next to Blocks and Outlines, off by default. The setting is remembered between sessions.
- Each zone face (or block) gets its signed pressure, e.g. "−42.3", at its centroid. The label faces the camera and is sized to the face: it is hidden when the face is too small on screen, so labels never pile up.

## 2. Sheathing types and the capacity calculations
- The Design tab offers every material in the tables. Each wall picks a **material per face**: none, wood structural panel (grade + thickness), wood panel over gypsum, gypsum wallboard, gypsum sheathing, gypsum lath and plaster, cement plaster, or fiberboard. The nail and fastener choices come from the selected table row.
- **Capacity per face** from Table 4.3A, 4.3B or 4.3C, with each table's own conditions (blocked/unblocked, stud spacing, aspect-ratio limits per Table 4.3.4, species factor for WSP).
- **Two faces — decided by the user, 2026-10-31,** from AWC and WoodWorks sources (below):
  - **Same material and construction on both faces:** 2 × one face (SDPWS 2021 4.3.5.4.1, Eqs. 4.3-3/4.3-4).
  - **Different materials (default):** the larger of the stronger face and 2 × the weaker face (SDPWS 2021 4.3.5.4.2). The same rule applies to stiffness.
  - **Wind exception, a project setting (on by default for new projects; the user's usual practice is to add the two faces):** wood structural panels, hardboard panel siding or structural fiberboard on one face with gypsum wallboard on the other may be summed for wind. WoodWorks lists all three; the AWC FAQ names only wood panels with gypsum, so check this against the printed SDPWS. The report says which rule was used.
  - **No presets by wall type** (user: exterior walls can be anything). Each face is chosen freely, with a "same on both faces" shortcut and a project default material.
  - **Two layers on the same face:** only the Table 4.3B assemblies (wood panels over gypsum) and the two-layer rows of Table 4.3C.
  - Sources:
    - AWC FAQ, "Is summation of capacities allowed for shear walls sheathed on both sides with different materials?" (2021-09-27).
    - WoodWorks Expert Tip, "Shear walls with multiple layers of shear resistance" (SDPWS 2021).
    - The section numbers differ between the two sources (4.3.3.3.2 and 4.3.5.4.2). The repository tables are typed from the 2015 edition (see `SDPWS_EDITION.md`).
- Hand-calculation unit tests for each table, as for Table 4.3A.

## 3. Drawing area
- **Typed input.** A command line under the drawing, as in AutoCAD:
  - Absolute point: `12,30`.
  - Relative: `@10,0`.
  - Polar: `@10<90`.
  - Length along the cursor direction: `10`.
  - Enter accepts, Esc cancels.
  - The live coordinates of the cursor show in the status bar.
- **Object snaps (OSNAP).** Endpoint, midpoint, intersection, perpendicular and nearest-on-object, each turned on or off from a snap bar.
  - The active snap shows a marker on the drawing: a square for endpoint, a triangle for midpoint, an X for intersection.
  - F3 toggles all snaps (as in AutoCAD).
  - The snap targets are walls, diaphragm edges and, later, the walls of the level below.
- **Click to inspect.** Clicking a wall or diaphragm selects it and opens a side card with its design: sheathing, nailing, v and capacity, hold-downs, range of shear with cases, and pass/fail. The card has links to the Design tab row and the calculation sheet.
- **The drawing shows the design:**
  - Wall line style or colour by sheathing type, plus a tag such as "W3 · 15/32 SI · 4" o.c. · 2 sides".
  - Pass/fail colour.
  - A **hold-down symbol at each end**, inset by half the required post width (the hold-down's minimum wood width from the catalog), labelled with its model. These marks are what a second story aligns to.

## 4. Clean-up of the legacy pages, errors and logging
- Restyle the legacy pages (wall and diaphragm data, results controls, load dialog, image dialogs, menus, toolbar) to the newer pages' look:
  - The same fonts, colours, spacing and table style.
  - One non-scrolling screen at the in-app size.
  - Labels with units.
  - Live updates, with no "send" buttons.
- Replace the toolbar's coloured buttons with a labelled tool bar (Select, Wall, Diaphragm, Snaps, Measure, Image) and a status bar (mode, cursor coordinates, snaps, messages).
- **Errors:**
  - One unhandled-exception handler for the UI thread and tasks. It logs the full detail and shows a plain message ("Something went wrong drawing the wall. The details are in the log.").
  - Input errors show beside the input, not in pop-ups.
- **Logging:**
  - A small logger writes to `%AppData%\ShearWallCalculator\logs\swc-YYYY-MM-DD.log`, kept for 14 days.
  - Each entry has a time, level and message.
  - Help > Open log folder.
  - It replaces the `Console.WriteLine` calls.

## 5. Image and drawing import
- **Images (PNG/JPG/BMP):** two-point calibration.
  1. Click two points on the image.
  2. Type the real distance (e.g. 24'-6").
  3. The image is scaled and placed. A third point can set rotation, so a plan can be aligned to the grid.
  - The image can be locked, dimmed (opacity slider), moved and re-calibrated. It is saved with the project as a path plus its calibration.
- **AutoCAD drawings (DXF):** a built-in DXF reader, with no external software.
  - It reads lines, polylines, arcs and circles, and the drawing units (`$INSUNITS`). The plan comes in **at true scale with no calibration**.
  - Layers can be turned on and off. The geometry is snappable (endpoint, midpoint, intersection), so walls can be traced exactly over it.
  - A later step could turn selected lines on a "walls" layer into walls directly.
  - DWG is a closed format; save it as DXF from AutoCAD (SAVEAS > DXF).
- **PDF:** reading vector PDFs needs a PDF library, which the "no downloads" rule excludes. For now, export from AutoCAD as PNG and calibrate, or save as DXF.

## 6. Multi-story: data, wind tributary areas, and carrying shears down

### Data model
- A project has **levels**, numbered from the ground up.
  - Level *i* holds the walls of story *i* and the **diaphragm at the top of those walls** (the floor above, or the roof at the top level).
  - Each level has a name, a top-of-wall elevation, a wall (plate) height and a diaphragm type (rigid or flexible).
- An old project opens as one level, so no file is broken.
- **Drawing:**
  - A level selector above the drawing.
  - The level below is shown ghosted under the current one, with its walls and hold-down marks, so walls can be stacked with object snaps.
  - "Copy walls from the level below" starts a new level.

### Wind on each diaphragm (tributary strips)
- The wind calculator (W7 core) is used directly. It gets the story heights, and the walls are cut into **horizontal strips**, one per diaphragm:
  - Level *i* (a floor) takes the wall area from the mid-height of story *i* to the mid-height of story *i+1*.
  - The roof diaphragm takes the area from the mid-height of the top story up to the roof, plus the roof's horizontal components and the parapets.
  - The bottom half of the first story goes straight into the foundation.
- **Chapter 27:** windward pressure follows qz(z), integrated over each strip; leeward and side walls use qh. Each Fig. 27.3-8 load case gives each level its own (Vx, Vy) and its own torsion (0.15 B × that level's force for Cases 2 and 4, plus each case's eccentric forces).
- **Chapter 28:** the zone pressures are uniform with height, so each zone's force is split between the strips by area; the torsional zones are handled the same way.
- Every load case is applied to **all levels together** (one case name, one direction), so the shear ranges stay consistent from top to bottom.
- New W7 core function: `StoryForces(building, levelHeights, case)` → forces per level per case. Unit tests check it against hand integrations of qz.

### Carrying shears down (top-down, per load case)
1. **Top level:** the roof diaphragm load goes to the top-story walls (rigid or flexible, as now).
2. **Each level below:** the diaphragm load is its own tributary wind **plus the reactions of the walls above**: each upper wall's shear, applied with opposite sign at that wall's line and location.
   - Walls that stack carry their shear straight down through the diaphragm.
   - Walls that don't stack (discontinuous) have their shear redistributed by the diaphragm below. This is the real load path, with no special case.
3. **Rigid diaphragm:**
   - The upper-wall reactions are point loads at known positions, so the level's resultant and its torsion about the CR come out directly: Mt = Σ(r × F) over all loads on the diaphragm, plus the case torsion.
   - The story shear is the sum of all levels above.
4. **Flexible diaphragm:** the tributary distribution takes point loads as well as the uniform strip. An upper wall's reaction goes to the two wall lines on either side by simple-span statics. This needs a small change to the support distributor, with hand-calculation tests.
5. **Overturning and hold-downs:** for each wall, T = M / L, using the story's own shear and height, less dead load.
   - Where the hold-down below is within a tolerance of the hold-down above (the marks from §3), the upper tension is **added** to the lower hold-down. The strap ("to wall below") family is the connection between floors.
   - A hold-down above with nothing under it is flagged: a discontinuity that needs a beam or post.
6. **Checks on each level's sheet:**
   - Σ wall shears = −(story shear).
   - Moment about the CR = −Mt.
   - Σ upper reactions delivered = Σ upper wall shears.

### Phases
1. Levels in the data model, project file and drawing (ghosted level below, copy walls).
2. Wind per level (W7 core `StoryForces`, both chapters) with tests.
3. Top-down cascade, rigid diaphragms, with the per-level sheet.
4. Flexible diaphragms with point loads.
5. Stacked hold-downs, cumulative tension and discontinuity flags.

### Questions for later
- Floor framing depth between levels: count it in the story height, or as its own strip?
- Dead load per level, for the overturning relief: a per-wall line load (as now), or from a floor load × tributary width?

## Suggested order
1. W7 PSF labels (small).
2. Logging and the error handler, then the clean-up of the legacy pages. Everything after builds on a tidy base.
3. Drawing tools: typed input, object snaps, click to inspect.
4. Sheathing types and capacities (after the 4.3.3.3 answer).
5. The drawing shows the design, with hold-down marks.
6. Image calibration and DXF import.
7. Multi-story, phases 1–5.
