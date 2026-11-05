# Roadmap / Parking Lot

Ideas and unfinished work across the suite (BuildingModel, Wind Load Calculator, Load Manager,
Shear Wall Calculator, WoodFraming, NDS_WoodLibrary). Pick items off one at a time; move them to
**Done** with the date and commit when finished.

Status tags: `[ ]` not started, `[~]` in progress, `[x]` done.

## Next up

- [~] **Central hub (launcher) for the apps.** The prototype is built: the new repo `..\DesignHub`
  (`DesignHub.exe`), branch `dev/hub-prototype`. See its `README.md` and `docs/DESIGN.md`.
  - [x] Opens a project, shows the five steps of the work with each one's state, and the next step.
  - [x] Launches each app on the project, one at a time.
  - [x] Access in one place (`IAccessPolicy`; everyone authorized for now).
  - [x] Flags the out-of-date foundation forces.
  - [x] Close project (title bar, the project page, Ctrl+W): back to the start page; asks first when an app the hub
    opened still has the project.
  - [ ] The owner to click through it.
  - [ ] Access from a real license or user file (`ListedApps` is ready; `HubSettings.Access()`).
  - [ ] Open an app on a given page (`--page foundation` for the Load Manager's foundation view).
- [x] **Merge on save, atomic saves, a shared project lock** (2026-11-05; BuildingModel `063084e` and the commits
  after it). Every app re-reads the file at save and writes only its own part: the Building Model merges three-way
  (`ModelFile.SaveMerged`), the Shear Wall Calculator replaces only its keys (`SaveOwnData`), the wind and the loads
  only their sections. Saves keep a `.bak`. Each app holds `<model>.<app>.lock`; the hub reads the locks.
- [x] **One owner per piece** (2026-11-05). The geometry is edited only in the Building Model (SWC's sketch is
  `GeometryLocked`); the wind only in the Wind Load Calculator (SWC's wind page is read-only, with Edit wind).
  The hub's steps: 1 Building, 2a Gravity loads, 2b Wind, 3 Shear walls, 4 Foundation loads.
- [x] **Shear walls are declared in SWC** (2026-11-05). A wall is a plain wall until declared there (its card: Make it a
  shear wall, then the shear part from–to); the hold-downs go at the part's ends. The Building Model has no shear-wall
  switch; its shear-wall checks moved to SWC (`ShearWallAdapter.Checks`). Older projects keep their marked walls, whole.
- [ ] **More than one shear part on a wall**, and a part per story (today one part, the same on every story the wall
  spans). For now, split the wall in the Building Model.
- [ ] **Show the declared shear walls in the Building Model** (read-only, e.g. a thicker line on the part), so the
  geometry editor shows where the hold-downs will be. Needs a small neutral reader the Building Model may use.
- [ ] **A suggested shear part:** the full-height stretches between doors and windows, offered when a wall is declared
  (the model has no wall openings yet: its openings cut floors).
- [ ] **Relax the hub's one-app-at-a-time rule** now that saves merge (for example, allow the Load Manager beside the
  Shear Wall Calculator), and let each app warn from the locks instead.
- [ ] **SWC clean-up after the geometry lock:** remove the dead geometry handlers (Export/Apply change set, Copy from
  below, Move and others); show the stud size in the construction window and the Members-tab coordinates read-only
  rather than refusing the edit.
- [ ] **SWC to watch the project file always** (today only while Edit building, loads or wind is open), so a change
  saved by another app reloads it.
- [ ] **Stamp stored results with what they were based on** (model revision, inputs hash), as `Lateral` does with
  `ModelRevision`, so the hub can say exactly what is out of date.

## Future modules

Shown greyed as "coming" in the hub (`SuiteApps`, in `..\DesignHub\DesignHub.Core\SuiteApps.cs`).

- [ ] **Material takeoff (MTO).** Count the lumber (studs, plates, headers, joists, rafters), sheathing panels,
  nails and hold-downs from the building model and the stored designs (the shear walls' sheathing, nailing and
  hold-downs; later the framing). Reads only; a report or CSV in the project folder. It needs the designs stamped
  with what they were based on (see above), so a takeoff is not made from out-of-date designs. The framing counts
  belong in `WoodFraming`, not in an app. In the hub as "Material Takeoff" (id `takeoff`).
- [ ] **Gravity framing** (joists, rafters, beams, columns) and **Foundation design** (footings). Already in the hub
  as planned.

## Shear Wall Calculator

- [ ] The next pass on the Shear Wall Calculator (scope to be agreed).

## Wind Load Calculator

- [ ] Open buildings (Ch. 27 Figs. 27.3-4 to 27.3-7, net pressure CN; 27.1.5 minimum on Af).
  The "Open" enclosure choice is greyed out ("Not implemented at this time") until then.
- [ ] Flexible buildings (Gf, 26.11.5) and the flexible-building eccentricity of 27.3.5. Only rigid
  buildings (G = 0.85) are handled now; the setup page says so.
- [ ] Chapter 28 parapets (28.3.2 in 7-16, 28.3.4 in 7-22). Now the Ch. 27 parapet force is carried.
- [ ] 26.13.1 reduction factor Ri for large-volume partially enclosed buildings (not applied now,
  which is conservative).
- [ ] 28.3.5 longitudinal loads on open or partially enclosed buildings with transverse frames.
- [ ] Domed, arched and mansard roofs (Figs. 27.3-2, 27.3-3; Fig. 27.3-1 note 6).
- [ ] Published examples: at least three worked examples written to ASCE 7-16 and three to ASCE 7-22,
  each run on its own edition. The current three (Quimby 7-05, SkyCiv 7-10, Medeek 7-10) run as 7-16.
  Candidate found: SkyCiv "ASCE 7-16 Wind Load Calculation for L-shaped Building" (Ch. 27, partially
  enclosed, metric, Ke = 0.987).
- [ ] C&C (Ch. 30): use the eave height for h at θ ≤ 10° where the figures say so (now the mean roof
  height, conservative); the "a" ≤ 0.8h exception for θ ≤ 7° and least dimension over 300 ft (not applied,
  conservative); 7-16 Figs. 30.3-2E/2F note 8 (B for Zone 3 vs Zones 1/2e; one h/B used now).
- [ ] 7-22 hip roofs θ ≤ 7° with overhangs: Table 30.7-1 sends hip roofs to roof + wall GCp; the code
  uses the Fig. 30.3-2A overhang curves (as for gables). Decide which applies.

## Done

- [x] 2026-11-05: ASCE 7-16 audit of Ch. 26 (the parts used), Ch. 27 (Directional) and Ch. 28
  (Envelope) against the code PDF. The values matched; the Ch. 28 section references now follow
  the edition (7-16 Fig. 28.3-1 notes 3, 5 and 8; 7-22 28.3.2 / 28.3.2.1).
