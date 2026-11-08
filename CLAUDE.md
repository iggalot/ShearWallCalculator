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
    foundation, with each point's hold-down (`Connector`), and every story's hold-downs (`Holdowns`, `StoryHoldown`:
    each wall's, and the beam ends and column bases carrying them down, with what each anchors to). SWC relays; the
    shared sketch draws the hold-downs on every level's plan, its own and those from above (`ConnectionMarkers` on Ground,
    `LevelMarkers` above; `Sketch.Connections` gives it SWC's live ones before a save). SWC draws only the beam-end
    reactions and the beam ends with no uplift itself. Each story hold-down keeps its force per wind case (`Up`: + tension,
    − compression, both ends of every couple), the compression carried through beams and columns as the tension (in bearing, not
    the hold-down), each wall's ends its own statics with everything landing on it (`DesignPass.CaseForcesOn`) and where its end presses (`CompressionX`/`Y`); each shear wall its wind shear
    on top per case (`ShearWallDesign.ShearOnTop`, `HeightFt`). Every case is saved once for each way the walls share it, `<name> (rigid)` and `<name> (flexible)`
    (`LateralSection.Cases`, `WindOf`): two analyses of one wind, never mixed (owner 2026-11-07), so every free body balances. The Load Manager makes every connection a pair of equal and
    opposite forces from them (`ConnectionPairs`) and checks each member's free body (WISE: the wind is an external load).
    Each level's own wind on its diaphragm per case goes in too (`Lateral.Diaphragms`, `DiaphragmWinds`, from
    `CaseBreakdown.OwnLoads`: at the center of mass, the torsion in the rigid cases), so every diaphragm's free body is
    checked with the walls' shears as pairs (`ShearPairs`: top, and sill into the floor or the anchor bolts).
    An upper story's sill nailing is designed (`ShearWallCheck.SillNailingOf`, `WoodFraming.SillNailing`: TR12 with the
    floor sheathing as the gap, the owner's choice; `ModelDefaults.FloorSheathing`, `SillNail`) and saved on each wall
    (`ShearWallDesign.SillNails`); the construction window's Pieces tab shows its check and sources.
    Each diaphragm's lines of shear walls are designed by the line shear, v = R / the diaphragm's depth along the line
    (owner 2026-11-07), with the boundary nailing (`Design/DiaphragmNailing.cs`: SDPWS-2015 Tables 4.2A and 4.2C, the
    `Table4_2Manager` and `Table4_2CManager` data) and the top plates as collectors (`WoodFraming.TopPlateCollector`):
    `DiaphragmLines.Design`, saved as `Lateral.DiaphragmLines`. The defaults are `ModelDefaults` (`DiaphragmGrade`,
    `FloorNail`, `FloorBlocked`, `RoofSheathing`, `RoofNail`, `RoofBlocked`, `DiaphragmFramingWidth`, `SpliceNail`).
  - **Conservative, kept by the owner (2026-11-07):** a beam end's or column's connection is sized for the hold-downs'
    tension alone, no dead load against it (`LoadPath`); the Load Manager's `UpperUplift` gives the net. Change it only if
    it becomes an issue.
  - **The wall window is shared** (`WoodFraming.Wpf` `WallPanel`, made by `WallViews.FromModel` from the model and a
    `Lateral` section): the Building Model's wall window and SWC's construction window (`Dialogs/WallBuildWindow.cs`,
    which adds its Pieces and Calculation tabs) draw the same thing. SWC hands it its live design
    (`ShearWallCheck.Lateral`, whose `Walls` are `ShearDesigns`: every declared wall, designed or not). A change to how a
    wall is drawn goes into WoodFraming.Wpf, not this app.
  - **SWC writes only its own data** (`OwnsData` in `MainWindow.Project.cs`): `ShearWall.Project`,
    `ShearWall.DesignSettings`, the walls' and diaphragms' `ShearWall` design, and `Lateral`. `SaveProject` uses
    `ModelFile.SaveOwnData`: it re-reads the file and replaces only those keys. It never writes the geometry, the wind or
    the loads. If the building changed meanwhile, the project is reloaded after the save.
  - **The geometry is the Building Model's.** `Sketch.GeometryLocked` is on and the drawing tools are hidden. Move,
    copy, delete, notes, cuts and copying a level are done there (Model > Edit building).
  - **The wind page is read-only** (`WindSetupControl.ReadOnly`, under a bar with **Edit wind**). It shows the
    wind calculator's Results page only (read-only stays on Results: no input pages). SWC opens on the wind when the
    project has no current wind (`WindCheck`: none, or it cannot be calculated), else on the shear walls (`OpenOnWorkspace`). Edit wind opens
    `WindLoadCalculator.exe` on the project and waits, as Edit building does. A project with no `Wind` section has no
    wind here: the walls use manual loads until the wind is set up.
  - **One SWC per project:** it holds the `shearwalls` project lock (`ProjectLock`).
  - **The design runs with no window too:** `ShearWallCheck.Run(building)` (ModelAdapter) is the window's design pass
    (`Design/DesignPass.cs`) on a copy, with the wind from the `Wind` section. The hub runs it in the background. Keep the
    window and it on the same code: a design change goes into `DesignPass` or `ShearWallCheck`, not the window.
  - **The wind comes from `WindCheck.Run`** (the wind calculator's own headless run) in the headless check; the
    Load Manager's foundation and the hub use the same. The hub chains them in `ProjectCheck` (DesignHub).
  - **The file is watched the whole time** (`WatchProject`, `ProjectChangedOnDisk` in `MainWindow.Project.cs`): another
    app's save reloads (when nothing is unsaved) and names the walls that newly fail. Nothing is saved in answer.
- **Line endings:** `ShearWallVisualizer\MainWindow.xaml.cs` uses LF.
