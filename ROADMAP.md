# Roadmap / Parking Lot

Ideas and unfinished work across the suite (BuildingModel, Wind Load Calculator, Load Manager,
Shear Wall Calculator, WoodFraming, NDS_WoodLibrary). Pick items off one at a time; move them to
**Done** with the date and commit when finished.

Status tags: `[ ]` not started, `[~]` in progress, `[x]` done.

## Next up

- [x] **The Ground plan shows what stands on it** (2026-11-06, BM 24fb4c4): the walls and columns whose base is the
  lowest level, drawn cut at their base (a double line). Select and edit only; no drawing on Ground (walls are drawn
  on the level they carry). It is a view only: the same objects, no copies, counted once by their story. Tied to the
  lowest level (`IsFoundationPlan`), not the name "Ground", for a later basement. Doors and windows are edited in the
  wall, later.
  - [x] The hold-down marks on it (2026-11-06, SWC 36f821f): SWC relays each point's hold-down into `Lateral`
    (`Connector`); the shared sketch draws them on Ground (`Sketch.Connections`), SWC draws nothing there.
  - [x] One way to draw hold-downs in every app (2026-11-06): `BuildingModel.Sketch.HoldownMarks` (the marker, its
    label kept off the others, an arrow to it); SWC's plans and the shared sketch's Ground both use it. Each marker is
    exactly where the hold-down goes: on the end post's inside face, a full post in from the end of the shear part
    (`WallSchedule.HoldownPoints`). Where two are close (a corner), each label's arrow tells them apart.
- [x] **The overturning arm with the hold-down on the post's face** (2026-11-06, owner: `L - 1.5 post`, the tension at the
  hold-down on its post's inside face, the compression at the far post's centreline). Was: The hold-downs now sit a full post
  in from each end, but the design's arm is still `L - post` (post centreline to post centreline, `ShearWallDesign.cs`).
  Tension at the hold-down on the inside face and compression at the far post's centreline would be `L - 1.5 post`;
  rod to rod, `L - 2 post`. `MomentFromAbove` uses half a post too.
- [x] **Wall joints** (2026-11-07, owner): one rule for every app (`WallJoints`, BuildingModel.Core). At a corner the
  longer wall runs past the shorter, an E-W one past an N-S one of the same length; at a tee the wall that ends part way
  along the other butts into it. The engineer turns a corner round with Flip in the wall's properties (only the turned
  ones are kept, `Building.FlippedJoints`, format version 7). A wall that butts stops at the other's face (half its stud
  depth, `StudSizes` now in BuildingModel.Core, WoodFraming reads it there); the plans show a bar where it stops. A shear
  wall's part, and its hold-downs, start at the face (`ShearWallAdapter.BuiltEnds`). Walls that cross are flagged.
  - [ ] Quick building and a DXF import drawing corners with their joints in mind.
- [ ] **Drawing walls on a DXF** (owner's question, 2026-11-07: plans rarely show centrelines). Suggestions:
  - [ ] Draw by a face: the wall tool's option centreline / left face / right face; an exterior wall drawn along its
    outside face puts the centreline half the stud depth inside.
  - [ ] Snap between two lines: where the DXF shows a wall as two parallel lines 3.5 to 7.25 in apart, a snap to the
    line midway between them.
  - [ ] Walls from the DXF: pick a layer, find its pairs of parallel lines, propose walls on their centrelines to accept
    or reject one by one.
- [x] **Hold-downs on every level in the Building Model** (2026-11-07, done 2026-11-07): SWC saves every story's
  hold-downs (`LateralSection.Holdowns`, `StoryHoldown`: connector, tension, post, what each anchors to) into the model;
  the shared sketch draws them on each level's plan (`SketchCanvas.LevelMarkers`): the walls' own, those coming down
  from the story above (a triangle, "↓ W7 1.5k onto W3"; red when nothing is below), the beam ends and column bases
  carrying them on. SWC's level plans draw them with the same routine.
- [x] **One wall window for every app** (2026-11-07, done 2026-11-07; owner: "a single routine for behavior across all
  of these apps"): `WoodFraming.Wpf` `WallPanel` (Elevation, 3D) and `WallWindow`, made from the model and its `Lateral`
  section by `WallViews.FromModel`. The whole wall's framing (`WallFraming.Build` with the shear part: the hold-down posts
  at its ends, a stud at the wall's own ends); the hold-downs always, with those from above; **Shear wall** (on by
  default) draws SWC's design over it (`LateralSection.Walls`, `ShearWallDesign`: the part, the faces' sheathing and
  nailing, the arm, the check, the sill bolts). The Building Model opens it from a wall's properties (Framing and 3D…);
  SWC's construction window hosts the same panel and adds its Pieces and Calculation tabs. Views only: nothing in them
  is calculated.
  - [ ] SWC's Pieces tab still lists the shear part's pieces; the whole wall's would match the views.
  - [ ] An undesigned shear wall (no wind yet) shows no sheathing: its declared faces could be drawn before the design.
- [~] **Every connection as a pair of equal and opposite forces** (owner, 2026-11-07). The Load Manager reports every
  connection between members: the connector's force once (tension or compression, per wind case), and on each member
  as a direction (↓ on the upper, ↑ on the lower). Every member then has its whole free-body diagram (its own loads and
  its connections' forces) and is checked to balance; a cut at any level is the sum of the pairs across it, so the
  levels can be split for analysis and verification. The foundation is the bottom row (its report unchanged). Who does
  what: SWC designs the hold-downs and saves the forces at both ends of each couple (its own data); the Load Manager
  reads them, adds the gravity and roof uplift it already has, makes the pairs and checks each member; the sketch draws
  them. Two kinds: a point pair (a hold-down, a compression post, later a rafter end) and a line pair (per ft along a
  wall: today the roof diaphragm on its top walls, a continuous connection).
  **WISE applies to every free body** (owner, 2026-11-07), one member or a whole system: the wind is an external load (E)
  and is never dropped; the connections are its reactions, and whether one is internal (I), a support (S) or applied (E)
  depends only on where the body's boundary is drawn and which members are in it. **The body is the selected objects**
  (owner): every member connected to it that is not selected gives its reaction on the body, and those are the internal
  forces (I) that appear because the view is split, so only one half of each pair shows. A pair with both members selected
  is inside the body: its halves cancel. S is the foundation; E the loads applied (the floors, the roof, the wind); W the
  weight.
  - [x] 1. (2026-11-07, BM 5f55eed, SWC cd84027, hub 4f46745; the rest in the step 2 commits) SWC saves each story
    hold-down's force per wind case (`StoryHoldown.Up`, W at strength level, + tension / − compression: the couple's two
    ends), where an end presses (`CompressionX`, `CompressionY`: the end post's centreline, the arm L − 1.5 post), and each
    shear wall's wind shear on its top per case and its height (`ShearWallDesign.ShearOnTop`, `HeightFt`).
  - [x] 2. (2026-11-07) The pairs (`BuildingModel.Model.ConnectionPairs`, one derivation for every app) and each member's
    free body with the wind on it in the Load Manager (`FreeBody.Connections`, `Shears`: E the wind's shear on its top;
    its connections I where it is cut, S on the foundation), checked in every case: ΣF, ΣM
    (`ConnectionBalance`, `FreeBody.WindOutOfBalance`; in `FoundationCheck.Run` and on the foundation page). The free body's
    side panel lists them case by case. The foundation's report is unchanged.
    - [x] (2026-11-07) The compression goes down too (owner: "it just doesn't use the hold-down to do it": the hold-down's
      bolt, or the sill pushing on the floor framing into the wall below; a point load all the same, the other way; tension
      or compression by the wind's direction). The load path carries each point's signed force per case (`CaseUp`) through
      beams (`Σ F (L − a) / L`, both signs) and columns; an end that only presses lands in bearing (no hold-down to size).
      Each wall's end forces are its own statics (`SetCaseTensions` with `DesignPass.CaseForcesOn`): its couple, and every
      force from above shared between its ends by the lever rule, each end at its hold-down where it pulls and its post's
      centreline where it presses. The hold-down is sized for the worst of those cases when that is more than the design's
      envelope (`AtLeastTheWorstCase`). Test: a shear wall on a beam, every member and the whole in equilibrium in every case.
    - [x] (2026-11-07) Rigid and flexible kept apart (owner): the same wind, two ways of sharing it among the walls, never
      mixed (a point taking the larger of the two put two analyses in one free body: a torsion case 270 lb out). Every
      case is saved under "<name> (rigid)" and "<name> (flexible)" (`LateralSection.Cases`, `WindOf`); the Load Manager
      takes each wind once for each, with the same roof pressures.
    - [x] (2026-11-07) The roof's wind on the same free bodies, and the gravity with the wind in the W combinations: a
      free body in one wind case (`FreeBody.OfBody(..., uplift, windCase)`, `WindCase`) takes that case's uplift pass, its
      wind alone in the same parts (the roof's on a top wall E, the cut's I, the foundation's S), and the shear walls'
      connections in it at the combination's W factor (`ConnectionsUp`, in `Imbalance`). The load path is linear, so each
      part balances and so does every combination (test: a wall, a stack and the whole structure, ΣFz in every
      combination with W). The window: the wind case picked under Wind is the W of the combination list; an uplift drawn
      away from the face it acts on. A wall's own couple is +T and −T: nothing in ΣFz, its check is ΣM under Wind.
    - [x] (2026-11-07) The body is the selection (`FreeBody.OfBody`): every force knows the member at its other end
      (`Other`; the gravity load path now records it: `ConcentratedLoad.Source`, `Onto`, `WallTopLoad.AboveParts`), so a
      wall under it or standing on it, a beam end, a column base are I (the cut), the foundation S; a pair between two
      selected members is inside (`Inside`, out of the sums). The gravity side too: a wall's base on a wall or beam is I,
      split by what is under each stretch. The wind's ΣF and ΣM about all three axes (any set of members). The whole
      structure is checked as one body too (`ConnectionBalanceResult.Whole`).
    - [x] (2026-11-07) In the Load Manager: Ctrl or Shift + click members in the 3D foundation (orange), then click or
      Free body of them; Ctrl + click takes one out. The cut drawn in purple in 3D, the pairs inside in grey.
    - [x] (2026-11-07) Every load on a beam on its own (owner: "you don't just take the largest... you take each individual
      case and solve for reactions"): a wall standing on it step by step along its profile (`Partials`, as onto a wall), the
      beam's own floor or roof share step by step (`BeamLoad.FromLevelProfile`), each point load; the reactions by statics
      of each, summed, each kind and each wind case's pass alike. Test: a beam under a wall with a roof zone over part of
      it and a floor zone over part of the beam: ΣFz, ΣM about its start, the pair with the wall place by place; by hand
      3200 lb at 13.875 ft in D (the largest over the wall would have been 5000 lb).
    - [x] (2026-11-07) ΣM for the gravity too (it was ΣFz only): `FreeBody.Moment`, `OutOfBalance` (the wind's rule, 0.5 %),
      each load at its own place (a wall on a beam or a wall a distributed load step by step, a post or beam end a point),
      with the wind case's shear walls in the combinations with W. `GravityBalance` checks every member and the whole
      structure in every combination (in `FoundationCheck.Run`, and on the foundation page); the free body's table gives
      ΣM. Test: a load moved 3 ft along a beam keeps ΣFz right and fails ΣM.
    - [x] (2026-11-07, already in step 5's commits) The hub's foundation step shows the gravity check's problems
      (`ProjectCheck.OutOfBalance`: `Gravity` and `Connections`).
  - [x] 3. (2026-11-07) The sketch's level labels from the pairs: several landing within 1 ft on one member in one label,
    the sum case by case, its worst case in ASD (`ConnectionPairs.Near`, `Sum`; in BuildingModel.Core, since the sketch
    cannot read the Load Manager: the same 1 ft as the Load Manager's `Near`).
    - [x] **An Internal forces toggle** in every drawing of the forces (owner, 2026-11-07): `SketchCanvas.ShowInternalForces`,
      by one rule (`ConnectionPairs.Drawn`). On (the default): both halves of every connection, each on its plan (the
      visual check). Off, for reports: the selection is the body (none: the whole structure); a connection inside it is
      not drawn, one at the cut only on the body's member, one on the foundation on the body's member; one with nothing
      under it always. Building Model: View > Internal forces; SWC: SHOW Internal; Load Manager: View > Internal forces,
      one setting with its free body's box.
  - [ ] 4. The roof ties as the top row: a line pair today (uplift − 0.6D per ft along each top wall, the diaphragm being
    continuous). **Decided (owner, 2026-11-07): the ties carry the C&C uplift where it is higher than the MWFRS uplift in
    the same place (the worst case).** The MWFRS uplift stays in the load path (the walls, the foundation); the tie's own
    design takes the larger of the two along each top wall, and says which one governs where.
    - [x] (2026-11-07) `RoofTie`: the C&C at the rafters' area and the MWFRS case lifting it most at the same wall or beam,
      both kept (`Cladding`, `Mwfrs`, their cases), the worse the tie's (`Line`, `Governs`); with no C&C, the MWFRS alone.
      Wind uplift window: both columns and which governs. Tests: C&C 45 over MWFRS 35 psf, MWFRS 35 over C&C 25.
    - [ ] The tie as a line pair on the free bodies (the roof a member: its half up, the top wall's down), per wind case
      (the MWFRS: the C&C is the tie's design, not a case of the whole building). Today the roof is not a member: its wind
      is E on the top walls.
    - [x] (2026-11-07) The tie's own value along a wall (the C&C zones change along it): `RoofTie.Along` (`RoofTieStep`), cut
      wherever the C&C or any MWFRS case changes, the worse of the two at each place (the owner's rule), the whole tie still
      its worst place. Wind uplift window: a collapsed table of the ties that change along their length. Test: C&C −45 psf in a
      6 ft corner zone, −20 beyond, the MWFRS −35: 0 to 6 ft the C&C −108 plf, 6 to 30 ft the MWFRS −72.
  - [x] 5. (2026-11-07) Upper-floor beams and columns carrying hold-downs from above: their uplift checked from their
    pairs (`UpperUplift`, in `FoundationCheck.Run`, on the foundation page): at each beam end and column base above the
    foundation, in every combination with W and every wind case, the net force on its support: its gravity reaction (each
    load on its own), the roof's wind in the case, the hold-downs' pair at the W factor. Where it pulls up with no
    connection, a problem (tie it down). Test: the wall on a beam, by hand 0.6 × 900 − 562 = 22 lb up at each end in
    0.6D + 0.6W (SWC's ST9 there sized for the 562 alone), the same as the beam's own free body gives.
    - [ ] SWC sizes a beam end's or column's connection for the hold-downs' tension alone: CONSERVATIVE, kept so by the owner
      (2026-11-07) for future consideration: it could take the dead load (the net, the Load Manager's `UpperUplift`;
      ASCE 7 lets the dead load resist at 0.6D). Change it if it becomes an issue.
  - [x] 6. (2026-11-07) Horizontal pairs at the same joints: the shear through the sill nailing and the anchor bolts.
    Each shear wall story's shear is two pairs (`ShearPairs`, BuildingModel.Core): its top into its diaphragm (the boundary
    nailing into the top plates), its sill into the floor under it (the sill nailing) or the foundation (the sill's anchor
    bolts, as designed). SWC saves every level's own wind on its diaphragm per case (`Lateral.Diaphragms`, `DiaphragmWind`:
    at the center of mass, with its torsion in the rigid cases). Every diaphragm's free body (its own wind, the walls under
    it pushing back, the walls above standing on it) is checked in every case: ΣFx, ΣFy, and ΣMz where the analysis puts
    the wind at a known place (rigid; the flexible run spreads it by tributary width: ΣF only), in `ConnectionBalance`, so
    the hub fails a diaphragm out of balance. The walls' free bodies name the connector at the top and the sill. Hub test:
    the box, both diaphragms in every case, the first story's shear = level 2's wind + the roof's by hand, the torsion taken
    out fails ΣMz, the roof's wind doubled fails ΣFx.
    - [x] (2026-11-07) The sill nailing of an upper story's shear wall designed (`WoodFraming.SillNailing`): common nails
      through the floor sheathing into the rim or blocking, Z by AWC TR12 Table 1-1 with the sheathing as the gap (the
      owner's choice: NDS 12.3.1 needs the members in contact, SDPWS is silent), Z' = 1.6 Z, the spacing Z'/v. Defaults
      (now `ConnectionDefaults`) `FloorSheathing` 23/32" and `SillNail` 16d; the app's limits 16" most, 3" least (not code values).
    - [x] (2026-11-07) The diaphragm's boundary nailing and the collectors designed, by the line shear (owner's choice):
      each line of shear walls in a level's diaphragm takes R per case; v = R / the diaphragm's depth along the line
      (`DiaphragmLines`, saved as `Lateral.DiaphragmLines`; now `Connections.DiaphragmLines`); the nailing from SDPWS-2015 Table 4.2A (blocked) or 4.2C
      (unblocked, Cases 2-6), ASD / 2.0 (`DiaphragmNailing`); the top plates a collector for the diaphragm's even delivery
      less the walls' shear parts (`WoodFraming.TopPlateCollector`: one ply in tension and compression, the splice nails).
      Defaults: floors 23/32" Sheathing and Single-Floor, 10d, blocked; the roof 7/16", 8d, unblocked; 2" framing; 16d splices.
      Table 4.2C's Structural I 8d 3" row was filed as 5/16" (fixed: 3/8", as printed); both tables tested row by row.
    - [x] (2026-11-07) The connection details moved out of the building model (owner: the model is the structure and
      geometry, the details that hold it together are separate, no god class): `..\WoodFraming\WoodFraming.Connections`,
      its own "Connections" section (the sills' bolts and nailing, the diaphragm lines; SWC writes it) and
      "ConnectionDefaults" (the sheathing, nails, blocking, framing width, splice nail), keyed by the model's ids,
      references one way only. The hold-downs and the force pairs stay in the model's `Lateral` (they carry the load).
      Older files' `Lateral` sill and line fields and `ModelDefaults` detail fields are ignored and drop on the next save.
    - [x] (2026-11-08) A window to edit the connection defaults, part of the connection details (owner):
      `WoodFraming.Wpf.ConnectionDefaultsWindow` (the choices with table rows; a hand-edited value not offered is kept;
      Starting values). SWC opens it (Details > Connection defaults), one undo step, saved with its own data.
    - [x] (2026-11-08) A shear wall's face nailing moved to the Connections (owner: the nailing is a detail, the sheathing
      and the shear stay in the model): `ConnectionsSection.Walls` (`WallNailing`, a `FaceNailing` per face in the
      order of `ShearWallDesign.Faces`); `ShearFace` keeps the sheathing, thickness and gypsum. The wall window reads both.
    - [x] (2026-11-08) A collector where no wall is under the line (the walls stand on the floor only): the floor's rim,
      one sawn ply of the joists' depth (`ModelDefaults.FloorJoistSize`, 2x10 to start; the framing's species and grade),
      in tension and compression (`WoodFraming.RimCollector`, CP 1.0, CD 1.6), and a strap across each butt splice for
      the whole force (`DiaphragmLine.StrapLb`). The NDS library now has 2x10 and 2x12 (Table 1B; Table 4A's CF). Hub
      test: a wall on a beam, v × 6 ft by hand.
    - [ ] The rim splices' strap picked from the catalog (MSTA, CS16...): the catalog page copied in first.
    - [ ] Southern Pine 2x10 and 2x12 (NDS Supplement Table 4B): not read into the NDS library yet (the rim check says so).
    - [ ] A line partly over walls: the stretch past the lower walls' ends is the rim too (today the plates are checked
      for the line's worst force all along it).
    - [ ] A page to edit the Building Model's defaults (`ModelDefaults`: stud sizes, the floor joists): only in the file.
    - [ ] The diaphragm's field nailing zones and chords (the line's nailing is given at the line only).
    - [x] (2026-11-08) The rim (a floor) or the roof's blocking toe-nailed into each shear wall's top plates
      (`WoodFraming.RimToPlate`, saved as `Connections.Tops`): the owner's choices, NDS Figure 12A's geometry for the yield
      equations (ts = (L/3)/cos 30° in the rim, the rest into the plate; the NDS gives no rule) and the wall's own ASD shear
      over its shear part (conservative); Z' = Z × 1.6 × Ctn 0.83 (12.5.4.2); the starting toe-nail 8d
      (`ConnectionDefaults.ToeNail`, in the defaults window). The top pair's words name it; the construction window and
      the wall elevation show it.
    - [ ] Framing clips (A35, LTP4) where the toe-nails need closer than 3": the catalog page copied in first, as the
      hold-downs' pictures were.
    - [ ] The horizontal pairs drawn on the plans and in the Load Manager's free body of a diaphragm (a diaphragm is not a
      member you can pick yet).
  - [ ] 7. With joists and rafters framed under the diaphragm: the roof tie line pair becomes a point pair at each rafter
    end (its tributary width); the points along a wall must add up to the line load they replace, case by case.
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
  - [x] Keeps the project in spec (2026-11-06): the stamps on the foundation forces (`Lateral.BasedOn`, by
    `ModelFingerprint`), the shear walls designed again with no window in the background (`ShearWallCheck.Run`), a red
    "Fails" step, a problems banner with "Show W4" (opens SWC at the wall, `--select`), and the problems marked in the file
    (`Hub.Status`: since when, seen by whom). SWC watches the file the whole time and names the walls that newly fail.
  - [x] One person per project; the rest look only (2026-11-06): the hub's own lock, `ProjectLock.HeldByOthers`; the apps
    refuse a project someone else has, and the hub opens it view only (no apps, no writes).
  - [ ] **View-only mode for the apps** (the owner's idea, 2026-11-06): someone else's project opened in an app to look at
    only: the plan, the results and the reports, no edits and no saves (the Building Model's and SWC's tools off, the
    Load Manager's grids read-only, no lock taken). Today they refuse it.
  - [x] Every app's calculation re-run with no window, in order (2026-11-06): `WindCheck` (the wind calculator's),
    `ShearWallCheck` on that wind, the hold-downs now against saved (by how much), `FoundationCheck` (the Load Manager's)
    on the forces as they are now; `ProjectCheck` chains them. The wind app and the Load Manager watch the file too, and
    the Load Manager says when the saved shear wall forces are older than the building or the wind. Uplift on the
    foundation is a flag for the foundation design (amber), not a fault.
  - [ ] Show the hub's problems in the apps too (SWC: the walls the hub marked, on opening).
- [x] **Merge on save, atomic saves, a shared project lock** (2026-11-05; BuildingModel `063084e` and the commits
  after it). Every app re-reads the file at save and writes only its own part: the Building Model merges three-way
  (`ModelFile.SaveMerged`), the Shear Wall Calculator replaces only its keys (`SaveOwnData`), the wind and the loads
  only their sections. Saves keep a `.bak`. Each app holds `<model>.<app>.lock`; the hub reads the locks.
- [x] **One owner per piece** (2026-11-05). The geometry is edited only in the Building Model (SWC's sketch is
  `GeometryLocked`); the wind only in the Wind Load Calculator (SWC's wind page is read-only, with Edit wind).
  - [x] SWC's wind page is the Wind Load Calculator's results only (2026-11-06; asked for earlier and missed): read-only
    `WindSetupControl` opens on Results and stays there (no Site or Building pages, no step bar, no Back or Next). No wind
    yet: the page says so (no starting values shown as if they were the project's). SWC opens on the wind when the project
    has no current wind (none, or it cannot be calculated: `WindCheck`), else on the shear walls.
  The hub's steps: 1 Building, 2a Gravity loads, 2b Wind, 3 Shear walls, 4 Foundation loads.
- [x] **Shear walls are declared in SWC** (2026-11-05). A wall is a plain wall until declared there (its card: Make it a
  shear wall, then the shear part from–to); the hold-downs go at the part's ends. The Building Model has no shear-wall
  switch; its shear-wall checks moved to SWC (`ShearWallAdapter.Checks`). Older projects keep their marked walls, whole.
- [x] **One selection for the whole suite** (2026-11-05): the shared sketch's (click, Shift/Ctrl+click, a window, Ctrl+A,
  Delete), with hooks for each app's own objects. SWC: one card for several walls; Load Manager: several walls get one top
  load, several zones are deleted together (its own picking removed).
- [x] **One undo for the suite** (2026-11-06; BM b53b8c1, LM 701b858): `EditHistory` in BuildingModel.Core is the one
  undo and redo, with named steps and the saved step. The Building Model and SWC use it through `ModelHistory`, and the
  Load Manager for its loads. The Wind Load Calculator has none (a plain form; the building is undone in the Building
  Model). With it, also shared once: `ProjectWatcher` (hub dd89159, LM, SWC's Edit building) and
  `ProjectDialogs.SaveChangesFirst` and `TakeLock` (BM, SWC, LM, Wind 9f7c920).
- [ ] **SWC's unsaved `*` from the history:** SWC still compares its whole saved project text, because its design
  also holds the calculators' loads that are not in the model's undo. Move it onto `EditHistory` once those are kept
  with the design.
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
  as planned. The roof questions it needs settled first (discussed 2026-11-06):
  - **A vaulted roof still has a diaphragm:** the roof sheathing is the diaphragm, sloped. The top level keeps its
    diaphragm; "vaulted" is a property of the roof, best per roof diaphragm piece (a vaulted great room, an attic
    elsewhere).
  - **The vault changes the gravity path:** no ceiling joists to tie the rafters, so a ridge beam and its posts down
    to the foundation (or rafter thrust on the walls). The framer and the Load Manager read the flag; the Building
    Model owns it.
  - **More than one roof** (`Building.Roof` is one shape, pitch and overhang for the whole building): a garage roof
    lower than the house, a shed beside a gable.
  - **Gable walls to the slope:** wall tops stay flat at their level. The gable triangle (wind area, wall weight) is
    derived from the roof by the apps that need it.
  - **Open to above** is already an Opening through the floors; a level with no diaphragm stays a warning
    (`NoDiaphragm`), with no flag of its own.
  - **Complex roofs:** dormers, extended hips, several gable walls, valleys.
  - **Vaulted ceilings in lower stories:** a raised ceiling inside a story (the floor above stays the diaphragm), or
    a two-story room (an Opening). They matter to the framing only at low clearances.
- [ ] **The vault and the shear walls (SWC):** the walls stop at the eave level, where the roof profile starts.
  Under a vault there is no ceiling at that level: the sloped roof is the diaphragm.
  - Model it as the top level's diaphragm: the roof's plan outline, with the vaulted flag on the piece. Removing it
    instead would give SWC no diaphragm, so no center of mass (today the area centroid of the level's diaphragm
    pieces, `DiaphragmSystem.ComputeCenterOfMassAndTotalArea`): the design stops.
  - The eave walls take the roof through the blocking. An interior wall under a vault that stops at the plate is
    not reached; one that rises to the roof (a bearing wall to the ridge) is. `ShearWallNotReached` must know it.
  - With part vaulted and part attic, the mass is not uniform: weight the center of mass by the dead load per piece
    (from the Load Manager), not the area.
  - The roof's shear and vertical loads act at the tops of the walls connected to it: the exterior walls, and an
    interior wall only where it rises to the roof (a high wall inside a vaulted room). The Building Model says which
    walls rise to the roof (geometry); SWC reads it.
  - The center of rigidity is taken from the walls the roof diaphragm reaches. The eccentricity (COM to COR, the
    wind's included) is between those two; a vault changes both, and never leaves the level without a COM.

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
