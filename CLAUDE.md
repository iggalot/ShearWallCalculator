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
  - **Hold-downs only:** SWC does no dead load, gravity or stud work. Gravity loads are edited in the Load Manager
    (Loads > Edit loads); SWC only reads them.
  - On save, the end couples per wind case go into the `Lateral` section, for the foundation.
- **Known issue:** `SaveProject` writes the whole file from memory, including the `Wind` section from its own copy.
  If another app saved the file meanwhile, its changes are lost. Edit building and Edit loads are safe because SWC
  waits for them. The Design Hub allows one app at a time per project. The fix (merge on save, a project lock) is in
  `ROADMAP.md`.
- **Line endings:** `ShearWallVisualizer\MainWindow.xaml.cs` uses LF.
