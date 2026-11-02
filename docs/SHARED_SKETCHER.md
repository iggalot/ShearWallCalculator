# The shared sketcher and the building model

_2026-11-01. Branch `dev/shared-sketcher` (from the clean commit `6f4be44`)._

The plan of the Shear walls workspace is now drawn in the **shared sketcher** (`BuildingModel.Sketch`) and kept in a
**building model** (`BuildingModel.Core`), both in the separate local repo `F:\Programming\BuildingModel`. The model is
the source of truth for the geometry: levels, walls, diaphragms, openings and notes. The calculators, tables, design,
loads and report work as before; they are filled from the model after every change.

## New projects used (all local, no remote)

| Project | Repo | Used for |
|---|---|---|
| `BuildingModel.Core` | `F:\Programming\BuildingModel` | the model, checks, edits, undo history, change sets, typed points |
| `BuildingModel.Sketch` | same | the drawing canvas, its toolbar and status line, error markers, issue list, 3D |
| `BuildingModel.ShearWalls` | same | `ShearWallAdapter`: model ↔ the calculators (one per story) |

They are `ProjectReference`s of `ShearWallVisualizer.csproj` and are listed in `ShearWallCalculator.sln` (folder
`BuildingModel`), so the solution builds them. Nothing else changed in the core library except the project file
(below). `LayerManager.cs` and `LayerData.cs` (the old drawing layers) are removed.

## How it fits together

```
 sketch edit / undo / redo ──► building model ──► ShearWallAdapter.ToCalculators ──► calculators (one per story)
        ▲                          ▲                                                       │
        │                          └──── StoreDesign (design tabs) / StoreGeometry (typed coordinates) ◄──┘
        └──────── overlays: design colours and marks, braced wall lines, COM / COR, plan image, error markers
```

- **Stories.** Each model level above the ground is one designer story: the walls under it and the diaphragm on it.
  The LEVEL buttons show the model's level names (Level 2, Level 3, Roof). A wall's height is from the level below
  that braces it up to the level (taller walls in a lobby are handled); the height column on Walls & diaphragms is
  read-only for that reason.
- **Keys.** A wall's calculator key is its ID number (W12 → wall 12 in the tables), the same on every story it spans.
  A diaphragm's is its ID number (D3 → 3), or 301, 302... when openings split it into rectangles.
- **Only what carries load.** A story gets the shear walls its net diaphragm reaches; openings come out of the
  diaphragm (mass and area). Other walls and openings are drawn but not analysed.
- **Design data** (sheathing, nailing, hold-downs per wall and story; diaphragm type; the project design settings)
  lives in the model's data bags (`ShearWall`, `ShearWall.DesignSettings`), so one undo history covers drawing and
  design. A new wall starts with the project's default design. Data filled in by the app itself after a change, an
  undo or a redo amends that step (`ModelHistory.Amend`), so it never costs the redo.
- **Loads and plan images** stay with each story's calculator (by level ID, so they follow the level when another
  floor is added or deleted).
- **Floors.** + Floor, ✕ Floor and Copy ↑ are model edits (Ctrl+Z undoes them, which it did not before). The number
  of floors and the plate heights go both ways with Wind Setup: changed in the drawing, the building inputs follow;
  changed in Wind Setup, the model's stories follow (asking before deleting a floor that has walls).

## What the drawing does now

The sketch's toolbar replaces DRAW / EDIT / Snap / Grid. The app toolbar keeps LEVEL, SHOW (Image ▾, Design), 3D,
the issues button and Loads.

- **Tools:** Select (click, window, grips to stretch), Wall (chained, square), Diaphragm, **Opening** (new: a hole open
  between two levels, such as a stair or a lobby), **Note** (new), Move, Copy, **Mirror** (new), Measure, **Split** and
  **Join** walls (new), Delete. Typed points in the status line box as before (`12,30`, `@5,0`, `@20<90`, a length,
  feet and inches).
- **Keys:** the sketch's (W wall, D diaphragm, O opening, N note, M, C, I mirror, T, X split, J join, Z fit, Del, Esc,
  F3 snaps, F8 ortho, Space / Enter repeat) and the app's old ones: **L** wall, **R** diaphragm, **S** snaps.
  The debug drawing moved from D to **F12** (D is Diaphragm in the sketch).
- **Error overlays:** red circles (errors) and amber triangles (warnings) on the plan, live: the model checks (walls
  on top of each other, a shear wall no diaphragm reaches, a level with no lateral support in one direction, openings
  at an edge, discontinuity...), **every wall that fails its design** (aspect ratio, shear or no hold-down strong
  enough), and a story that cannot be calculated yet. The toolbar's issues button counts them (✗ errors, ⚠ warnings,
  ✓ none); click it, or Model > Check the model, for the list; click an item to find it on the plan (on its level).
- **Wall colours:** green when its design check passes, red when it fails, blue before it is checked (as before).
- **Level caption:** the level, the story, its plate height and how many walls fail.
- **3D:** the 3D button (or Model > 3D view) shows the building from the model, the level shown stronger.
- **Plan image:** Image ▾ works as before (PDF, DXF, picture; scale with two points and a typed distance, straighten,
  move with snaps, opacity, lock), drawn under the plan; the DXF lines snap.
- **Selection:** picking on the plan selects in the tables and the card, and picking a table row selects on the plan
  (and brings it into view).

## Model menu

- **Open a building model (.bldg.json)**: replaces the plan with a building drawn in the Building Model app; design
  data stays where the model has it; Ctrl+Z undoes it.
- **Save the building model as**: the building for the Building Model app and the other analysis programs.
- **Export changes since opened**: a change set (added, moved, deleted, by ID) since the project was opened or saved.
- **Apply a change set**: from another copy of the model; conflicts are listed (apply anyway, skip them, or cancel).

## Project file

Format 3 is unchanged except for one optional field, **`Model`**: the building model's JSON. When present it is the
source of truth; the per-story calculators in the file still carry the loads and plan images (and let an older build of
the program open the file). Files without it (all older files) open as before: the model is made from their
calculators (`ShearWallAdapter.FromCalculators`), and it is saved with the project from then on. A file without a
model is written byte for byte as before.

## Known limits

- A diaphragm split by openings is edited on the plan, not by typing its corners in the table (the table edit is
  kept only for a diaphragm that is one rectangle on its story).
- Up to 3 floors for the wind split (`BuildingData.MaxStories`); a model with more stories is analysed per story but
  the wind is split over the first 3 (noted on the Calculations tab).
- Walls are W-E or S-N, as before.

## Tests

- SWC: 162 (new: `ProjectFileTests.BuildingModel_RoundTrips_AndCountsAsAChange`). The 161 before all pass unchanged.
- BuildingModel: 49 model / adapter tests (new for this step: typed geometry back to the model, the default design for
  new walls, new buildings and story names, copying a story's design up, history amend keeps the redo) and 11 sketch
  tests (new: the host hooks, with a rendered snapshot of a failing wall in red with its error marker and the caption).
- The app window itself is not opened by the tests; check it by running the app.
