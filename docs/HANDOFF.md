# Handoff: continuing the shared-sketcher work on another machine

_Written 2026-11-02. This file sums up the instructions, rules and state of the work so it can be picked up on another
machine (by a person or by Claude Code). Give it to the assistant at the start: "Read docs/HANDOFF.md and continue."_

## The request

1. Implement all the plans for the shared sketcher, the standalone Building Model app and the shear-wall adapter.
   Then switch the Shear Wall Calculator (SWC) onto the shared sketcher. The building model is the source of truth for
   the geometry, and the app must keep everything SWC already did.
2. Add drafting and construction features that would help the designer.
3. Keep the UI clean, intuitive and minimalist, with toolbars of small clean buttons.
4. **Error checking, error overlays and notes are required.**
5. Provide full documentation and testing. Unit-test each calculation against the code it comes from.

## Rules (still in force)

**Repositories and files**
- Do not push anything outside the local repository. Make local git repositories only, with no remotes.
- Do not change or edit anything outside the project directories in use. Do not write into any other project folder
  without permission.
- Do not download external applications. No NuGet or other external packages; the MSTest packages already in the local
  NuGet cache are allowed.
- New projects may be made as folders under `F:\Programming`, each with its own local git repo. Document any new project.

**Branches and builds**
- Any change to these projects starts from a clean commit, and all work is on a new separate branch.
- After any reshuffle, every project must compile.

**Way of working**
- Do not spin up external threads or shells for the work.
- Use the session scratchpad for temporary files, not `/tmp`.
- Never close the user's running app without permission.
- Never create `MainWindow` in a script or test. The user checks the window by running the app.
- Documentation goes in repo files (`docs/`).

**UI and data**
- The graphics and graphs may be improved visually, but the core data must stay.
- The app fits on one screen without scrolling at its normal size.
- Results and loads update live when inputs change.

## The repositories

| Repo | Branch | Last commits | What |
|---|---|---|---|
| `F:\Programming\ShearWallCalculator` | `dev/shared-sketcher` | `5096dda` (the switch), from clean `6f4be44` | The Shear Wall Designer (`ShearWallVisualizer`), its core library and tests |
| `F:\Programming\BuildingModel` | `dev/building-model` | `5b59549`, `7b35d35`, `7dca7bc` | The building model, sketcher, app and adapters (new projects, made for this work) |
| `F:\Programming\ASCE7-Wind-Load-Calculator` | (unchanged) | | The wind engine, referenced by both |

The repos have no remote. **To move to another machine, copy all three folders (with their `.git` folders)** to the
same parent folder. The projects reference each other by relative path (`..\..\BuildingModel\...`,
`..\ShearWallCalculator\...`, `..\ASCE7-Wind-Load-Calculator\...`). A `git bundle` of each repo also works.

## Build and test

These are Visual Studio 2026 (version 18) paths; adjust them on the other machine.

```
MSBuild:  C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe
vstest:   C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\Extensions\TestPlatform\vstest.console.exe

MSBuild ShearWallCalculator.sln -restore          (also builds the three BuildingModel projects it uses)
vstest  ShearWallCalculator.Tests\bin\Debug\...\ShearWallCalculator.Tests.dll
MSBuild ..\BuildingModel\BuildingModel.sln -restore
vstest  BuildingModel.Tests\bin\Debug\net472\BuildingModel.Tests.dll
vstest  BuildingModel.Sketch.Tests\bin\Debug\net472\BuildingModel.Sketch.Tests.dll   (set SNAPSHOT_DIR to get PNGs)
```

Expected: SWC 162 pass, BuildingModel 49 pass, Sketch 11 pass.

## Where things are

- `docs/SHARED_SKETCHER.md`: the architecture, keys, overlays, Model menu, project file, limits and tests.
- `docs/PROGRESS.md`: the history (newest at the top).
- `F:\Programming\BuildingModel\README.md` and `docs/DESIGN.md`: the model, data bags, edits and undo, the sketcher's
  host hooks, the app and the adapter.
- SWC code:
  - `MainWindow.Drafting.cs`: the sketch hookup, model ↔ calculators, issues, 3D and the Model menu.
  - `MainWindow.Levels.cs`: floors as model edits and the sync with Wind Setup.
  - `Interfaces/ProjectFile.cs`: the optional `Model` field.

## How it works (in short)

- **Model drives the calculators.**
  - Building model → `ShearWallAdapter.ToCalculators` → one calculator per story, kept per level ID (`calcByLevel`).
  - Design-tab changes go back to the model with `StoreDesign`; coordinates typed in the table go back with
    `StoreGeometry`.
- **Undo.**
  - One undo history (`ModelHistory`) covers drawing, design and floors.
  - Data the app fills in itself after a change, undo or redo uses `ModelHistory.Amend`, so it never loses the redo.
  - Undo replaces the level objects, so levels are compared by ID.
- **Keys.** A wall's calculator key is its ID number (W12 → 12). A diaphragm's is its number, or n·100+k when
  openings split it.

## Status and next steps

**Done.** All the requested work is implemented, documented, tested and committed.

**Still to do:**
1. **Run the app and check the new drawing by eye.** It has not been seen running yet. Check:
   - drawing walls and diaphragms;
   - undo and redo;
   - floors (+ Floor, ✕ Floor, Copy ↑);
   - the issues button and its markers;
   - the plan image tools;
   - opening an older project file.
2. **Known limits, possible next work:**
   - typing the corners of a diaphragm that openings split, in the table;
   - the wind split is limited to 3 floors;
   - walls only run W-E or S-N;
   - openings in walls (doors, windows);
   - the wind engine using each level's face shares;
   - seismic and dead/live load apps reading the same model.
3. When the branch is accepted, merge `dev/shared-sketcher` locally. Still no pushing.

## Practical notes (Windows, Claude Code)

- **Git.**
  - Run git as `git -c safe.directory=* ...` when the folders are owned by another user.
  - Commits end with a `Co-Authored-By:` line.
- **Line endings.**
  - `MainWindow.xaml.cs` uses LF line endings.
  - `Interfaces/ProjectFile.cs` mixes CRLF and LF, so edit it carefully.
- **Shell.**
  - No Python is used.
  - Bash heredocs that contain `'` break. Write scripts to a file and run them with perl.
