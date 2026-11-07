# Shear Wall Calculator: notes for assistants

- **Read first:**
  - `..\DesignHub\docs\SUITE.md`: all seven repos, how they fit, the branches, the build, and **the owner's rules**
    (local commits only, no installs, stay in the project folders, never click the owner's windows).
  - `docs/PROGRESS.md`: the history, newest at the top.
  - `docs/HANDOFF.md` is from 2026-11-02 and older: for the branches, go by `SUITE.md`.
- **Branch:** `dev/ui-pass-2`.
- **`ROADMAP.md` (at this repo's root) is the TODO and parking lot for the whole suite.** Add ideas there, and tick
  them off with the date and the commit. `docs/ROADMAP.md` is an older plan for the wind and shear-wall tool.
- **Projects:**
  - `ShearWallCalculator.csproj`: the core, old-style csproj.
  - `ShearWallCalculator.ModelAdapter`: the model to the calculators; the `ShearWall.*` data.
  - `ShearWallVisualizer`: the app `ShearWallVisualizer.exe`, old-style.
  - `ShearWallCalculator.Tests`.
  - Newtonsoft.Json comes from `packages\`.
- **The rules this app stands on:**
  - **No design without a valid building model.** No project, or a model with errors, gives no design, loads or
    reports.
  - **The wind is the wind calculator's `WindCases`** (by name, every case). SWC makes no wind of its own.
  - **Shear walls are declared here.** A wall is a plain wall (it carries its gravity load) until it is declared in SWC:
    its card's **Make it a shear wall**, then **Shear part** from–to (ft from its west or south end). The hold-downs go at
    the part's ends. It is `WallExtras.Shear` in the wall's `ShearWall` data (`ShearWallAdapter.Declare`, `Undeclare`,
    `ShearPartOf`). `ShearWall.Declared` on the building marks a project that declares; an older project is adopted once
    (`Adopt`: the walls it marked, `Wall.IsShearWall`, declared over the whole length). The shear-wall checks are
    `ShearWallAdapter.Checks` (no shear wall both ways, not reached, two overlapping: an error that stops the design).
    Several walls picked together (Shift+click, or a window) get one card: Make shear walls, Not shear walls, and one face 1
    sheathing and hold-down type for all (`DeclareAll`, `UndeclareAll`, `EditDesign`; each one undo step).
  - **Hold-downs only:** SWC does no dead load, gravity or stud work. Gravity loads are edited in the Load Manager
    (Loads > Edit loads); SWC only reads them.
  - On save, the end couples per wind case go into the `Lateral` section (`BuildingModel.Model.LateralSection`), for the
    foundation, with each point's hold-down (`Connector`). SWC relays; it draws nothing on Ground: the shared sketch
    draws the hold-downs there (`Sketch.Connections` gives it SWC's live ones before a save).
  - **SWC writes only its own data** (`OwnsData` in `MainWindow.Project.cs`): `ShearWall.Project`,
    `ShearWall.DesignSettings`, the walls' and diaphragms' `ShearWall` design, and `Lateral`. `SaveProject` uses
    `ModelFile.SaveOwnData`: it re-reads the file and replaces only those keys. It never writes the geometry, the wind or
    the loads. If the building changed meanwhile, the project is reloaded after the save.
  - **The geometry is the Building Model's.** `Sketch.GeometryLocked` is on and the drawing tools are hidden. Move,
    copy, delete, notes, cuts and copying a level are done there (Model > Edit building).
  - **The wind page is read-only** (`WindSetupControl.ReadOnly`, under a bar with **Edit wind**). Edit wind opens
    `WindLoadCalculator.exe` on the project and waits, as Edit building does. A project with no `Wind` section has no
    wind here: the walls use manual loads until the wind is set up.
  - **One SWC per project:** it holds the `shearwalls` project lock (`ProjectLock`).
- **Line endings:** `ShearWallVisualizer\MainWindow.xaml.cs` uses LF.
