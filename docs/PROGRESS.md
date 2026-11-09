# Progress Log

_Branch `dev/wind-core-refactor` in both repositories. Companion to `ROADMAP.md`, `AUDIT.md` and `DATA_VERIFICATION.md`._

## 2026-11-09 — The declared shear walls in the Building Model; the hub opens a step's page

- The shared plan bands each declared shear wall's shear part in blue, tagged SW (SW fails in red), from the `Lateral`
  section's walls (`SketchCanvas.ShowShearParts`): the Building Model and the Load Manager show where the hold-downs go.
  SWC turns it off (`MainWindow.Drafting.cs`): it draws its own walls, live. Sketch test: the band on a 20 ft part, and off.
- The hub opens each step's app on its page (`WorkflowStep.Page`, `Launcher.ArgumentsFor`): the Load Manager's step 2a on
  Loads, step 4 on Foundation (`--page foundation`, `MainWindow.ShowPage`). Hub tests: the steps' pages and the arguments.
- ROADMAP: SWC's watching the file from the open on was done with the hub's checks; ticked.

## 2026-11-09 — The horizontal pairs on the plans; a diaphragm's free body

- The shared plan draws each shear wall's horizontal pairs in teal (`SketchCanvas.DrawShearPairs`, with the hold-downs,
  `ShowConnections`): which half by the hold-downs' WISE rule (`ShearPairs.Drawn`), the worst case of each.
- A diaphragm picked on its level is a body (`ShearPairs.DiaphragmKey`): its own wind at its point and each wall's half on
  it, drawn in one case (`SketchCanvas.FreeBodyCase`, else the case with the most wind on it, `DiaphragmCase`).
- The Load Manager's Foundation page steps up through the levels (the level bar: the foundation, then each floor and the
  roof). Clicking a level's diaphragm lists its free body in the side panel, case by case (`ConnectionBalance.DiaphragmForces`):
  E its wind, I each wall's push, ΣFx, ΣFy, ΣMz (the check's sums), and whether it balances in every case.
- Tests: the drawing rule and the worst case (BM), the render with a picked diaphragm (Sketch), and Level 2's free body by
  hand in the Load Manager (1000 − 2000 + 1000 = 0; with 1500, ΣFx = +500 shows).
- Southern Pine 2x10 and 2x12 (NDS Supplement Table 4B, from the SFPA's 2013 guide kept in `NDS_WoodLibrary\docs\references`)
  and the Building Model's defaults page (Building > Model defaults) are in too (ROADMAP).

## 2026-11-09 — The splice straps and the framing plates picked from the catalog

- The strap across each splice of a rim or each joint of the roof's blocking (collectors and chords) is picked from the
  strap ties of C-C-2026 p. 291 (`Design/Ties.cs`, `SpliceStrap`): the lightest whose (160) tension takes the whole force,
  its width at most the member's depth less 1". Saved as `DiaphragmLine.Strap` and `DiaphragmChord.Strap`.
- Where the toe-nails would need closer than 3", LTP4 framing plates tie the rim or blocking to the top plates instead
  (C-C-2026 p. 310, configuration 7, direction G, (160): DF/SP 715 lb, SPF/HF 615; `FramingPlates_Manager`,
  `TopClip`, `ShearWallCheck.TopClipOf`, `TopConnection.Clip`): s = 12 A / v to the inch below, 6" to 24". On a face
  with no wood panel with 0.131 x 1-1/2 nails; a wall paneled on both faces over the thinner (1/2" at most) with
  0.131 x 2-1/2 nails at the full load (footnote 3). The pair's words, the wall elevation and the construction window
  show it.
- Tests: the straps by hand from p. 291 (ST2215, HRS6, MSTI36 vs. MSTC28 on a 2x4); the plate's spacing and placement.

## 2026-11-09 — The roof's blocking past the walls; the diaphragm's field nailing zones

- The owner's choices: on the roof, where no wall is under a line of shear walls or an edge, its continuous blocking
  between the rafters is the collector or chord (`ModelDefaults.RoofBlockingSize`, new, 2x6 to start), checked as a floor's
  rim is (`WoodFraming.RimCollector`, now naming its member), a strap across every joint for the whole force. The roof's
  lines no longer keep the plates past the walls' ends.
- Two nailing zones per span: from each line the diaphragm's shear falls linearly to nothing at mid-span (a cantilever's
  free end), starting from the larger of the line's own v and the span's 0.6 w (L/2) / d; the line's nailing runs out
  until v is under the lightest row of the sheathing (SDPWS Table 4.2A's 6", or 4.2C's one row), rounded up to the 2 ft,
  the lightest row beyond. Saved on each span (`DiaphragmChord`); the construction window shows them with its chords.
- Hub tests: the roof's line past a shortened upper wall, v × 10 ft, and past the wall at x = 20, v × 6 ft; the zones
  with 6d on 5/16" at 170 mph: 363.1 plf at the north-south lines, the lightest row 237.5 plf, 8 ft from each line.

## 2026-11-09 — The diaphragm's chords

- The owner's choices (2026-11-08): each diaphragm is a beam in simple spans between its lines of shear walls, loaded by
  its own wind (the worst case's force over its length), M = w L² / 8 at mid-span, a cantilever past the last line
  w a² / 2; T = C = M / d at its edges, d its depth there, ASD (0.6 W). The chord: the double top plate of the walls under
  the edge whose length covers the point (spliced with 16d each side, as a collector), else the floor's rim with a strap
  at each splice; the roof's edge with no wall under it is reported, not designed (ROADMAP).
- `DiaphragmLines.Chords` (ModelAdapter), saved as `Connections.Chords` (`DiaphragmChord`, `ChordsOn`); the construction
  window's Pieces tab lists the chords a wall's plates are, with the sources.
- Hub tests: the box, 40 × 30 ft: level 2 wind along x w = 393.4 plf, M = 0.6 × 393.4 × 30² / 8 = 26,551 lb·ft, T = 664 lb
  (3 16d each side of a 2x6 splice); along y T = 1,662 lb (8); the roof's 2x4. With the lower north wall out (a beam
  under the upper one) the north chord is the 2x10 rim, its strap 1,662 lb.

## 2026-11-09 — A floor line partly over walls: the plates, then the rim; 2x4 to 2x12 for any member

- Along a floor's line of shear walls the collector is the top plates within the length of the walls under it (none past
  a wall's end: the owner, 2026-11-09, after a first try that let them run 6" past it) and the rim past them; each is
  checked for the worst force on its own stretch (`DiaphragmLines`, both at the hand-off at a wall's end), the rim's
  strap for the rim's. The roof keeps the plates (ROADMAP).
- Hub test: a first-story wall under 12 of the 30 ft line at x = 20, a wall from 6 to 24 ft standing on the floor: 4,120 lb
  at the lower wall's end, in its 2x6 plates and the rim alike (the hand-off), both as N(t) recomputed in the test.
- NDS library (owner): `Table4.Sizes` 2x4 to 2x12 for any 2x member (studs, rims, joists, ceiling joists, rafters);
  the rim tested at every size by hand.

## 2026-11-08 — The floor's rim as the collector where no wall is under the line (step 6)

- A line of shear walls that only stand on a floor (none under it) has no top plates to collect into: the floor's rim is
  the collector. One sawn ply of the floor joists' depth (`ModelDefaults.FloorJoistSize`, new, 2x10; a framing default, so
  the model's), checked in tension and compression like the top plates (`WoodFraming.RimCollector`); its butt splices
  carry no tension, so each needs a strap for the whole force (`DiaphragmLine.StrapLb`; the strap is not picked yet).
- NDS library: 2x10 and 2x12 sections (Table 1B) and Table 4A's size factors for 10" and 12" wide; Southern Pine's wide
  rows (Table 4B) are not read in yet and say so.
- The construction window shows an upper wall's floor line too. Hub test: the wall standing on a beam at x = 20 (18 of
  30 ft): 56.4 plf, collector 338.6 lb = v × 6 ft; DF-L No. 2 2x10 rim 14,042 lb tension, 29,970 lb compression.

## 2026-11-08 — The rim or blocking toe-nailed into the walls' top plates (step 6)

- The owner's choices: toe-nails by NDS Figure 12A's geometry (12.1.6.3: 30°, started L/3 from the end; along the nail
  ts = (L/3)/cos 30° = 0.385 L in the rim, p = 0.615 L into the plate, as the NDS gives no rule for the yield equations),
  and the wall's own ASD shear over its shear part (conservative). Z' = Z × CD 1.6 × Ctn 0.83 (12.5.4.2); spacing Z'/v to
  the 1/2" below, the app's 16" most and 3" least. Hand check: 8d DF-L, Z 97.1 lb (mode IV), Z' 129.0 lb, 7-1/2" at 200 plf.
- `WoodFraming.RimToPlate`; `ShearWallCheck.RimToPlateOf` (one routine for the save and the window), saved as
  `Connections.Tops` (`TopConnection`: the roof's blocking or a floor's rim). `ConnectionDefaults.ToeNail` 8d, in the
  defaults window. The top pair's words, the construction window's Pieces tab (check and sources), the wall elevation.

## 2026-11-08 — The face nailing into the Connections; the connection defaults window

- Owner: the nailing moves into the connections, the sheathing and the shear stay in the model; the defaults editor is
  part of the connection details. `ShearFace` keeps the sheathing, thickness and gypsum; the nails and edge and field
  spacing are `ConnectionsSection.Walls` (`WallNailing`, `FaceNailing`, face by face in the same order:
  `ShearWallCheck.NailingOf`, one list of sheathed faces for both). The wall view takes `WallView.Nailing`.
- `WoodFraming.Wpf.ConnectionDefaultsWindow`: the diaphragms' grade, the floors' and roof's sheathing, nails and blocking,
  the framing width, the upper sills' nail, the splice nail; Starting values; a value not offered is kept. SWC opens it
  from Details > Connection defaults (one undo step; "ConnectionDefaults" added to its `OwnsData`).
- Tests: the face nailing's round trip and the wall view's; the window's values and starting values; the hub's each face
  nailed. All suites pass (WF 41, BM 99, Sketch 78, LM 59, SWC 228, Hub 58).

## 2026-11-07 — The connection details out of the building model (WoodFraming.Connections)

- Owner: the building model is the structure and geometry; the nailing, bolts and diaphragm patterns that hold it
  together are details and belong apart, or the model becomes a god class. The hold-downs stay: they carry the forces.
- New project `..\WoodFraming\WoodFraming.Connections` (references BuildingModel.Core only): `ConnectionsSection`
  ("Connections": `SillConnection` with `SillAnchors` or `SillNails`, `DiaphragmLine`, and `Describe`/`Describer` for a
  shear pair's words) and `ConnectionDefaults` ("ConnectionDefaults", the starting values in one place).
- Removed from BuildingModel: `ShearWallDesign.Anchors` and `SillNails`, `LateralSection.DiaphragmLines`, and the
  `ModelDefaults` detail fields. `ShearPairs.Of` takes a describer; each pair has `StoryLevelId`.
- SWC: `ShearWallCheck.Connections` builds the section (the headless run's `ShearWallCheckResult.Connections`, the
  window's save stamp, the construction window); `OwnsData` includes "Connections". The Load Manager and the hub word
  the pairs from it. Old files' removed fields are ignored and drop on the next save.
- Tests: WoodFraming's round trip, the words for each pair, the defaults and a missing value; the hub's assertions
  read the sills and lines from the Connections. All suites pass (WF 40, BM 99, Sketch 78, LM 59, SWC 228, Hub 58).

## 2026-11-07 — The diaphragms' boundary nailing and collectors, by the line shear (step 6)

- Owner's choice: the diaphragm's line shear plus collectors (not the wall's own shear). Each line of shear walls in a
  level's diaphragm (the walls under it and standing on it, within 0.5 ft across) takes R per case; v = R / depth along the
  line. The boundary nailing: SDPWS-2015 Table 4.2A (blocked) or 4.2C (unblocked, the lower Cases 2-6), ASD = nominal /
  2.0, footnote 2's SG factor; 2-1/2" or closer needs 3" framing (4.2.7.1.1). The collector: the diaphragm's even delivery
  less the walls' shear parts along the line; a double top plate checked one ply in tension and compression, the splice
  nails each side (NDS 12.3.1). The top pair's connector names it; the construction window shows it with its sources.
- Tables 4.2A and 4.2C tested against the printed pages row by row; 4.2C's Structural I 8d 3" row fixed (3/8", not 5/16").
- Hub test: the box with the south wall declared over its middle 20 ft: v × 10 ft by hand at each end of the part.

## 2026-11-07 — The upper sills' nailing designed (step 6)

- `WoodFraming.SillNailing`: an upper story's shear wall sill nailed through the floor sheathing into the rim or
  blocking. NDS-2018 12.3.1 needs the members in contact and SDPWS gives no rule for the gap, so (owner's choice) the
  nail's Z is AWC TR12 Table 1-1's, with the sheathing as the gap (`NDS2018.GeneralDowel`, checked against TR12's
  printed examples; with no gap it is Table 12N to the pound). 16d common through 23/32": Z 64 lb (mode IV), against 141
  lb with no gap. Z' = 1.6 Z; spacing Z'/v to the 1/2" below; the app's limits 16" most and 3" least are not code values.
- The defaults (`ModelDefaults.FloorSheathing` 23/32", `SillNail` 16d), the design saved on each upper wall
  (`ShearWallDesign.SillNails`), the sill's pair names it, and the construction window's Pieces tab gives the check and
  the sources. Hub test: every upper wall's sill nailed with 16d.

## 2026-11-07 — The roof tie place by place along its wall (step 4)

- `RoofTie.Along` (Load Manager): the tie cut wherever the C&C zones or any MWFRS case change along the wall or beam, the
  worse of the two at each place; the whole tie stays its worst place. The wind uplift window lists the ties that change
  (collapsed). Test by hand: a 6 ft corner zone, C&C −108 plf there, the MWFRS −72 plf beyond.

## 2026-11-07 — The horizontal pairs and every diaphragm's free body (step 6)

- Each shear wall story's shear is two pairs (`ShearPairs`, BuildingModel.Core): its top into its diaphragm, its sill into
  the floor (sill nailing) or the foundation (its anchor bolts). SWC saves each level's own wind per case
  (`Lateral.Diaphragms`: at the center of mass, the torsion with it in the rigid cases; `CaseBreakdown.OwnLoads`).
- `ConnectionBalance` checks every diaphragm in every case: ΣFx, ΣFy, and ΣMz in the rigid cases (the flexible run's load
  is spread by tributary width: ΣF only). Out of balance fails the hub's foundation step. Hub test: the box; the first
  story's shear is level 2's wind plus the roof's, by hand; the roof's torsion taken out fails ΣMz; its wind doubled
  fails ΣFx. Next: the upper sills' nailing designed, and the pairs drawn.

## 2026-11-07 — The hub fails a member out of balance; upper beams and columns carrying hold-downs (step 5)

- Conservative, kept by the owner for now: SWC sizes a beam end's or column's connection for the hold-downs' tension
  alone, no dead load (marked in `LoadPath`, `UpperUplift`, the foundation page and ROADMAP step 5); to change if it
  becomes an issue.
- The hub: a member not in equilibrium (the gravity's `GravityBalance` or the wind's `ConnectionBalance`) fails the
  foundation step, named on the problems list (`ProjectCheck.OutOfBalance`): a load not accounted for is a fault.
- Step 5 (`UpperUplift`, Load Manager): each beam end and column base above the foundation that carries hold-downs, in
  every combination with W and every wind case: its gravity reaction, the roof's wind, the hold-downs' pair at the W
  factor; the worst net force on its support, and where. Pulling up with no connection: a problem. The foundation page
  lists them. Hub test, by hand: 0.6 × 900 − 562 = 22 lb up at each end of the beam under a wall, the same as the beam's
  own free body.

## 2026-11-07 — ΣM for the gravity free bodies; every member checked

- The owner asked whether a wall on a beam or a wall is a distributed load in the checks: it is (its profile step by
  step, `Partials` on a beam, `AboveParts` on a wall; posts, beam ends and columns point loads). But the gravity checks
  were ΣFz only, blind to a load in the wrong place. Now `FreeBody.Moment` and `OutOfBalance` give ΣFz and ΣM (about the
  body's middle, the wind's 0.5 % rule), the shear walls' forces included in a combination with W. `GravityBalance`
  checks every wall, beam and column and the whole structure in every combination (`FoundationCheckResult.Gravity`; the
  foundation page says how many balance). Tests: the beam with zones (every member and the whole), a load moved 3 ft
  (ΣFz right, ΣM fails), the hub's box in every combination with W.

## 2026-11-07 — Every load on a beam on its own

- The owner: a beam can have several loads; never take the largest, take each on its own and solve its reactions (and
  free body) for each. The Load Manager's beam now takes a wall standing on it step by step along the wall's profile
  and its own floor or roof share step by step (`BeamLoad.FromLevelProfile`), as well as each point load; its reactions
  are the statics of each, summed (`GravityLoadPath.Reactions`), and its free body shows the same steps. SWC's hold-down
  trace already did this case by case (`LoadPathTrace`: each load's lever-rule share per case, summed).
- Test: a beam with a roof zone over part of the wall on it and a floor zone over part of it: ΣFz and ΣM about its
  start in every gravity combination, the pair with the wall equal and opposite; by hand 3200 lb at 13.875 ft in D.
- The free body's profile steps now keep the roof's wind apart too (`AddProfile`: a step with wind only is kept, and
  two steps are joined only when their wind is the same).

## 2026-11-07 — Roof ties the worse of C&C and MWFRS; the gravity and the wind on one free body

- Roof ties (owner): the C&C uplift where it is higher than the MWFRS uplift at the same wall or beam, else the MWFRS
  (`RoofTie.Cladding`, `Mwfrs`, `Governs`; with no C&C, the MWFRS alone). The Wind uplift window shows both and which
  governs.
- A free body in one wind case (`FreeBody.OfBody(..., uplift, windCase)`): its gravity loads, the roof's wind in that case
  from its uplift pass (the same forces again, each with its wind alone, `ServiceLoad.PathWind` → `Winds`), and the shear
  walls' connections in it at the combination's W factor (`ConnectionsUp`). Its combinations now include those with W.
  The load path is linear, so every combination balances (hub test: a wall, a stack, the whole structure).
- Load Manager window: the wind case picked under the free body's Wind is the W of its combination list (and the title);
  an uplift is drawn away from the face it acts on.

## 2026-11-07 — The compression goes down too; rigid and flexible kept apart; every member in equilibrium

- The owner: the compression is transmitted as well, "it just doesn't use the hold-down to do it" (the hold-down's bolt,
  or the sill pushing on the floor framing into the wall below): a point load like the tension, the other way, either one
  by the wind's direction.
- SWC's load path (`LoadPathTrace`) carries each point's signed force per case (`CaseUp`, + tension / − compression)
  through beams (the lever rule, both signs) and columns. A beam end that only presses lands in bearing: no hold-down to
  size, not an error.
- Each wall's end forces per case are its own statics (`WallDesignResult.SetCaseTensions` with
  `DesignPass.CaseForcesOn`): its couple, and every force from above (the wall ends over it, the beam ends and column
  bases on it) shared between its two ends by the lever rule, each end at its hold-down where it pulls and at its post's
  centreline where it presses (where the Load Manager puts it). Where that worst case is more than the design's envelope
  (the walls above taken at their worst, scaled by this wall's case), the hold-down is sized for it (`AtLeastTheWorstCase`).
- Rigid and flexible (the owner's choice): the same wind, two ways of sharing it among the walls, saved apart as
  "<name> (rigid)" and "<name> (flexible)" (`LateralSection.Cases`, `WindOf`). The Load Manager takes each wind once for
  each, with the same roof pressures. A point taking the larger of the two had put two analyses in one free body (a
  torsion case 270 lb out of balance).
- Test (hub): a shear wall standing on a beam, the beam on the walls below: every member and the whole structure balance
  in every case (ΣF, ΣM), the beam's ends pass on all that lands on it, a beam end presses in some case.
- Roof ties (owner): the C&C uplift where it is higher than the MWFRS uplift, the worst case. In the ROADMAP, step 4.

## 2026-11-07 — The body is the selection (WISE everywhere); the Internal forces toggle (step 3)

- The owner's rule: a free body is the selected objects; every member connected to it that is not selected gives its
  reaction on the body, and those are the internal forces (I) that show because the view is split, one half of each pair.
  A pair between two selected members is inside the body and cancels. S is the foundation, E the applied loads (the
  wind among them, never dropped), W the weight.
- The Load Manager: `FreeBody.OfBody` (any set of members). Every force knows its other member (`Other`); the gravity
  load path now records where each load comes from and goes (`ConcentratedLoad.Source`, `Onto`, `WallTopLoad.AboveParts`),
  so a wall's base on a wall or beam is I (split by what is under each stretch), the walls standing on it I, a beam's ends
  on columns I, the foundation's S. The wind's balance is ΣF and ΣM about three axes; the whole structure is checked as
  one body as well. In the window: Ctrl or Shift + click members in the 3D foundation (orange), then click or Free body
  of them; Ctrl + click takes one out; the cut in purple, the pairs inside in grey (with the internal forces shown).
- The plans (the shared sketch): `ShowInternalForces` by one rule (`ConnectionPairs.Drawn`). On: both halves of every
  connection. Off: the selection is the body (none: the whole structure), only its halves at the cut and on the
  foundation. Hold-downs landing within 1 ft on one member share a label with their sum case by case (`Near`, `Sum`:
  SWC's old grouped labels, back for every app). Building Model and Load Manager: View > Internal forces; SWC: SHOW
  Internal.
- Tests: `FreeBodyTests` (the beam and column as one body; `StackedWalls_TheCutMovesWithTheSelection`), the balance
  tests with the whole structure, `LevelPlan_InternalForces_TheBodyIsTheSelection` (Sketch).

## 2026-11-07 — Every connection a pair of equal and opposite forces (steps 1 and 2)

- The owner's plan (ROADMAP): the Load Manager reports every connection between members as a pair of equal and
  opposite forces, and checks every member's free body. WISE for every body, one member or a whole system: the wind is an
  external load and is never dropped; whether a connection's force is I, S or E depends on the body's boundary.
- SWC saves, case by case, what the free bodies need: each story hold-down's force (`StoryHoldown.Up`, W at strength
  level, + tension / − compression, both ends of every couple, from `EndForceByCase`; a beam end's or column base's
  tensions), where an end presses (`CompressionX`/`Y`, the end post's centreline), and each shear wall's wind shear on its
  top and its height (`ShearWallDesign.ShearOnTop`, `HeightFt`, from the new `WallDesignResult.ShearByCase`). The rigid and
  flexible shares govern the same way everywhere (`ShearWallCheck.Bare`, `Govern`).
- `BuildingModel.Model.ConnectionPairs` makes the pairs (one derivation for every app: the Load Manager's free bodies now,
  the plans' drawing and the Internal forces toggle next).
- The Load Manager's `FreeBody` has the wind's side (`Connections`, `Shears`); `ConnectionBalance` checks every wall, beam
  and column in every case: ΣFx, ΣFy, ΣM. In the box test every member balances (ΣM within rounding, e.g. 28 lb·ft of
  99,250). The free body's side panel lists the wind forces case by case with the three sums; the foundation page says how
  many pairs and members, and names any that do not balance. The foundation's own report is unchanged.
- Known: SWC's load path carries only tension through beams and columns, so a wall end pressing on one would show as not
  balanced there (ROADMAP).
- Tests: `ConnectionBalanceTests` (Load Manager, 4, by hand: two stacked walls balance; a missing force and a landing on
  nothing show; a file saved before the cases has nothing to check), the hub's
  `EveryMember_BalancesTheWind_EveryConnectionAPair` and `Run_EveryStorysHoldowns_...` (the per-case forces, each wall's two
  ends a couple, the first story's as on the foundation).

## 2026-11-07 — One wall window for every app

- `WoodFraming.Wpf` has the wall window every app opens: `WallView` (plain data, made from the model and its `Lateral`
  section by `WallViews.FromModel`), `WallElevation`, `WallModel3D`, `WallPanel` (Elevation and 3D) and `WallWindow`.
- It draws the whole wall as built (from the faces of the walls it butts into), framed by `WallFraming.Build` with the
  shear part: the hold-down posts at the part's ends, a stud at the wall's own ends, the sheets over the part. The
  hold-downs are always drawn, each on its end post's inside face with its connector, tension and what it anchors to,
  and so is what comes down onto the wall's top from the story above (with a post under each).
- **Shear wall** (on by default) draws SWC's design over the framing: one face's sheathing and nails over the shear part,
  the sheathing and nailing callouts, the sill bolts, the lever arm, the check and the shear part's dimension. A plain
  wall says it is not a shear wall. Off, the framing and the hold-downs stay as they are.
- The design comes from the model: SWC writes `LateralSection.Walls` (`ShearWallDesign`, `ShearFace`, `SillAnchors`:
  every declared wall, its part as built, faces, nailing, arm, check, hold-down fasteners, a first-story wall's sill
  bolts), from `ShearWallCheck.ShearDesigns` in the window and the headless check.
- The Building Model opens it from a wall's properties (**Framing and 3D…**; `ObjectProperties.OpenWall`, which BM.App
  sets); it follows the pick and the model. SWC's construction window hosts the same `WallPanel` with its own Pieces
  and Calculation tabs; its own elevation and 3D classes are gone.
- The views draw the data and calculate nothing: no design value depends on them.
- Tests: `WallViewTests` (WoodFraming, 6), `Properties_AWall_FramingAnd3D_OpensTheHostsWallWindow` (Sketch), the hub's
  `SwcPicksAWall_ItsCardOpens` (the shared panel in SWC's window) and `Run_EveryStorysHoldowns_...` (the walls' designs saved).

## 2026-11-07 — Hold-downs on every level, in every app

- SWC saves every story's hold-downs into the model: `LateralSection.Holdowns` (`StoryHoldown`: where, the connector,
  the tension, the post, the level it is on and the one it lands on, and what it anchors to: the foundation, a wall, a
  beam or a column below, or nothing). `ShearWallCheck.StoryHoldowns` maps the load-path trace; the window and the
  headless check both write it.
- The shared sketch draws them on every level's plan (`SketchCanvas.LevelMarkers`; Ground keeps `ConnectionMarkers`):
  the walls' own hold-downs exactly at their points; those coming down from the story above as a triangle ("↓ W7 1.5k
  onto W3"; red, "nothing below", when there is no wall, beam or column under it); the beam ends and column bases
  carrying them on as their own connections. SWC's level plans now use it too (its own copies removed), so the Building
  Model and SWC show the same thing.
- Tests: `LevelPlan_ItsWallsHoldowns_AndThoseComingDownFromAbove` (Sketch), `Run_EveryStorysHoldowns_IntoTheModel_ForEveryLevelsPlan` (hub).

## 2026-11-07 — Wall joints: one runs past, the other stops at its face

- `WallJoints` (BuildingModel.Core), one rule for every app: at a corner the longer wall runs past the shorter, an E-W one
  past an N-S one of the same length; at a tee the wall that ends part way along the other butts into it. Flip in the
  Building Model's wall properties turns a corner round (one undo step; kept in `Building.FlippedJoints`, file format 7).
  The plans mark each joint with a bar across the wall that stops.
- A shear wall's part starts at the face of the wall it butts into (`ShearWallAdapter.BuiltEnds`), half that wall's stud
  depth short of the centrelines' point, so its length and its hold-downs are as built. The stud depths moved to
  BuildingModel.Core (`StudSizes`); WoodFraming reads them there.
- The corner warning is gone (every corner has its rule); walls that cross are still flagged (`WallsCross`).
- Fixed: undo in the Building Model did not bring back the roof, the defaults (or now the joints).
- Checked: a wall picked in SWC opens its card and its construction window, on a story and on Ground (a test with the
  real window). The Building Model has no wall window yet; one shared window is on the roadmap.
- Hold-down lever arm d = L − 1.5 post (owner): the tension at the hold-down on its post's inside face, the compression at
  the far post's centreline.

## 2026-11-06 — Hold-downs drawn one way, where they go; wall ends that take the same space

- The hold-downs are a full post in from each end of the shear part, on the end post's inside face
  (`WallSchedule.HoldownPoints`; it was half a post). That moves the marks, the foundation's anchor points (`Lateral`)
  and where the uplift lands below. The design's arm is now `d = L - 1.5 post` (owner): the tension at the hold-down on its post's inside face, the
  compression at the far post's centreline (`ShearWallDesign.cs`, the details' "Lever arm" line). The hold-downs get a
  little heavier (a 10 ft wall with a 3 in post: d 9.75 → 9.625 ft, T +1.3 %).
- One drawing of hold-downs in every app: `BuildingModel.Sketch.HoldownMarks`. Each marker is exactly at its point
  (as wide as the post where it shows, 6 to 10 px), and each label is beside its wall, kept off the others, with an arrow
  to its marker. SWC's plans and the Building Model's Ground plan both call it. Before, Ground drew one square per
  quarter foot with the text beside it, so W4's hold-down at a corner hid under W9's.
- The Building Model flags wall ends that take the same space: `WallEndsMeet` (an L drawn to one point: one runs past,
  the other stops at its face) and `WallsCross`.

## 2026-11-06 — The design with no window; the project kept in spec

- Fixed: a save with nothing changed put the shear walls and the foundation out of date in the hub. SWC takes the Wind
  section off its own copy of the building, so the stamp fingerprinted no wind; it now stamps the wind it used
  (`windJsonInUse`, `BasedOnNow` in `MainWindow.Lateral.cs`). The hub's `SwcSaveTests` opens and saves with the real
  window and checks the hub finds nothing out of date (and that the window's forces match the headless ones).
- The wind page is the wind calculator's results only (read-only stays on Results); SWC opens on the wind only when the
  project has no current wind, else on the shear walls. With no wind the page says so instead of showing starting values.
- Every app re-runs with no window, in order: `WindCheck` (ASCE7Wind.ModelAdapter), `ShearWallCheck` on that wind,
  `FoundationCheck` (LoadManager) on the shear walls' forces as they are now; the hub chains them (`ProjectCheck`) and
  compares the hold-down forces now with the saved ones. Uplift on the foundation is flagged for the foundation design.
  The wind app watches the file too; the Load Manager reads the `BasedOn` stamps.
- The design pass is out of the window: `Design/DesignPass.cs` (`TopDown`, `All`, `CheckLevel`) and, in the ModelAdapter,
  `ShearWallCheck` (`Run`: the whole design on a copy of a building, as the window does it, with the wind from the `Wind`
  section; `SetWindLoads`, `Framing`, `Lateral`, `InputsNow`). The window calls the same code. `WallDesignResult.FailureReason`.
- The `Lateral` section is stamped with what it was found from (`BasedOn`: the geometry, the wind, SWC's design inputs).
- SWC watches the project file the whole time. When another app saves and nothing is unsaved here, it reloads, designs
  again and names the walls that newly fail; with unsaved changes it only says so. `--select W4` opens it at a wall
  (the hub's "Show W4").
- The hub re-checks in the background after every read, shows a red "Fails" step and a problems banner, and marks the
  problems in the file (`Hub.Status`). Someone else's project is open view only in the hub; the apps refuse it.

## 2026-11-03 — The drawing key beside the plan

- The shear wall schedule and the Loads legend are no longer drawn over the plan. They sit in a drawing key under the
  right-hand tabs (schedule left, Loads right; the "From Level 3" line in orange, the wall-top note in blue). The key shows
  only on the Shear walls page, only for what is switched on (SHOW Design, SHOW Loads), and the tabs end above it. It is
  rebuilt only when its text changes (MainWindow.Loads `UpdateDrawingKey`). `DesignOverlay.Draw(..., schedule: false)`.
- Loads arrows (wall tops and the upper walls' shears) slide along their wall to the first spot clear of tags, labels and
  the schedule; the wind arrow shortens before it is pushed onto the building.
- The drawing key shows only while something is picked on the plan (the schedule is on the Design tab, each case on the
  Calculations tab).
- Beam and column labels on the plan: everything at one point is one label ("↓ 0.8k + 0.9k on column C1   BM2 start ↑ 0.4k
  HRS6"); the beam's mid-span summary is gone from the plan (Members tab and its card). The Members tab's beam row names
  each end's connection ("start 367 lb → column C1 HRS6, end 420 lb → column C3 HRS6").
- Hold-downs case by case where they share a support. Each wall's hold-down is still its worst case (its design shear,
  the envelope of every case and run), but two walls' worst cases are not at the same time (an E-W wall peaks in W/E
  cases, a N-S wall in N/S). `WallDesignResult.SetCaseTensions` gives each wall's tension in every case at the end that
  lifts (a negative force on the diaphragm pushes the top +, lifting the low end); the walls above are scaled by
  |V_case| / V, so the governing case is exactly T. The load path (`LoadPathTrace.SumByCase`) adds these case by case on a
  column (its base) and a beam (each end's reaction), and sizes for the worst case of the sum, not the sum of the worst.
  A wall-on-wall stack on the same line is unchanged (envelope, by wind direction). The plan says it:
  "↓ 0.8k + 0.9k (at once 0.9k) on column C1". Tests: CaseByCaseHoldownTests (by hand).
- A wall's hold-down type that cannot anchor where it lands (an HDUE into a beam) is replaced automatically by one that
  can (a strap), no longer a warning (an error only when none of that type is strong enough). The Design tab's Hold-down
  column says where ("DTT1Z · ST9 strap on beam BM2"), and the Hold-down type column is always shown, to override.

## 2026-11-02 — Load cases, wall by wall; the Loads overlay; columns on the foundation plan (BuildingModel 266db64)

- **Checked: every load case is its own simultaneous Vx, Vy, Mt, solved on its own,** in the rigid and the flexible
  solvers and both runs of the envelope.
  - The wind calculator sends ASCE 7 Fig. 27.3-8 Cases 1–4 with both wind signs and both torsion signs: 4 + 8 + 4 + 16 = 32 cases.
  - Each case's Mt,comb = Vx·ey − Vy·ex + Mt is formed inside that case, so a Vx of one case never meets a Vy of another.
  - The upper story's wall reactions join the case of the same name below (`StoryCascade.WithLoadsFromAbove`).
  - Each wall keeps its most + and most − shear and is designed for the larger magnitude. Both hold-downs take it,
    since the reversed wind lifts the other end.
- **`CaseBreakdown`** (calculator library, tested against the solvers in `CaseBreakdownTests`): every wall in every case
  (own load, from above, ex, ey, Mt,comb, direct, torsion, total, governs), and one case's loads from above.
- **Calculations tab:** "Background: every load case, wall by wall", **collapsed by default**. It is for digging into
  the numbers; the design table stays on top. It follows the selected wall or shows all walls, and a row click selects the wall.
- **Plan, Loads (off by default; ▾ picks the case, or pick it on the Calculations tab, and the two follow each other):**
  - Orange: the shear each upper wall puts into this floor.
  - Blue: what the top of each wall here receives.
  - The legend gives the case's own wind, the part from above, the total on the diaphragm and Mt,comb.
  - With no case picked, it shows the case that designs the most walls on the level.
  - The wind: a broad arrow outside the windward side pointing at the building; Cases 3 and 4 (quartering) come in at
    45° onto the windward corner ("WIND from SW";
    +x is from the west, +y from the south, `CaseBreakdown.WindFrom`), and for Cases 2 and 4 a curl at the center with
    the case torsion (CCW for + Mt).
- **Cases listed by wind direction** (`CaseBreakdown.Listing`): grouped W, SW, S, SE, E, NE, N, NW, then by case number,
  + torsion before −. Each is labelled with its direction first ("SW · Case 4, torsion +267.6 k·ft CCW"), in the Show list
  (with group headers), the Loads ▾ menu (a submenu per direction), the background table and the design table's Case
  columns. The case names stay the keys between levels.
- **Plan text 1.2× the original** (1.5 crowded the plan; BuildingModel `SketchCanvas.LabelScale`, one number): the sketch's IDs, lengths and notes and
  this app's overlays (loads, hold-downs, schedule marks and schedule, legend, wind). Labels above a mark grow upward.
- **Overlaps:** wall-mark tags try the other side and then slide along the wall to clear tags already drawn; the
  overlay labels (hold-downs, loads, reactions) step off the tags, the schedule and each other. The schedule's rows are as
  tall as their text, and it keeps clear of the grid numbers and scale bar (lower corners) and the level caption.
- **Junk, Level 3 W10 has no nailing dropdown:** its face 1 is gypsum wallboard (Table 4.3C row 6, fasteners fixed at 7").
  Untouched defaults follow a wall on or off the outside edge (the interior default is gypsum on both faces); growing D1
  to 59 ft did that to W3 and W11 at 17:46:27 with nothing in the log saying why. Now logged: the adapter says when a wall
  takes or switches to a default and why, and the Design tab logs any wall whose nailing cell has no choice and why (the
  cell reads "7" fixed", with the reason on hover).
- **Start-up status:** "No wind geometry from the model" before the project loads is now a note, not a warning that stays
  in the status bar; the bar says when the geometry arrives.
- **Foundation plan:** a column run down to the foundation is drawn there ("C1 base", outlined square) and can be picked.
  The same holds on any level a column stands on.

## 2026-11-02 — Walls over beams, walls past the floor's edge, the framing list, the warnings log (SWC 739dff4, 347262b; BuildingModel 31ac477)

- **Found on the junk project:** the three Level 3 notes on W6, W7 and W8 ("no wall under it") ignored the beams
  BM1–BM3 that carry them.
  - `StoryCascade.Unsupported` now counts the beams of the level below as well as its walls.
  - A support end within 3 in of the wall's end reaches it, since the beams were drawn 1/16 ft short at the column corners.
- **New check, `StoryCascade.OffFloor`:** the length of each upper wall beyond the diaphragm under it.
  - A beam or column carries the wall's weight and uplift, but not its shear.
  - It gives a note on the level and a `WallOffFloor` warning on the Design tab, which points at the wall.
  - In junk, the Level 2 floor stops at x = 46 while W6, W7 and W8 run to 59.5.
- **Walls & diaphragms tab, now Members:** every level, with a level filter (default: the level shown), sortable by level.
  - Four tables: walls, diaphragms, beams and columns.
    - Each has a Level column that sorts by elevation.
    - A column is listed at its top level and appears on every level it runs through.
  - Show: "This level" (the default, which follows the drawing), "All levels", or one level.
  - Each beam and column row shows what it carries in the load path.
  - A click shows the member's level and selects it on the drawing.
  - Typed wall and diaphragm coordinates go to that member's own level.
  - The × on any row deletes the member from the model on any level, without switching the drawing.
    - The whole wall goes when it spans stories, and a column goes through every story.
    - Every level's list follows, and Ctrl+Z brings it back.
- **Sketch (both apps):** a selected column gets a ring well clear of its square.
- **Log:**
  - `[Issues]` lists every error and warning with its code, level, members and location: all of them when a model
    loads, then `new:` / `resolved:`.
  - `[Design]` does the same for each level's message, the failing and close walls, and the level notes.

## 2026-11-02 — Wall, beam and column details, on screen and in the report (SWC)

- **Core `DetailCalcs`** (`CalcLine`: item, calculation, result). It restates the design and computes nothing new:
  - **Wall:**
    - V and v = 0.6 V / L against the capacity, and h/b.
    - M = V h; d = L − post; dead load; own uplift T = 0.6 M / d − 0.6 D / 2.
    - Each force from above with its position and wind direction, and what they add.
    - The total T and the hold-down with its ratio.
    - What each end anchors to (flagged when its type was replaced).
  - **Beam:** each load's share R start = T(L−a)/L and R end = T·a/L, summed per wind direction; the reactions; each end's support and connection.
  - **Column:** what lands on it, what goes down it, its base connection.
- **Drawings** (`Controls/MemberDetail.cs`, the same on screen and in the report):
  - `WallElevation`: to scale, with sheathing joints, the end posts, a hold-down at each post (model, T, what it anchors to) and the shear. The forces from above are arrows at their positions, blue or orange by wind direction. Dimensions for d, L and h.
  - `BeamDiagram`: the loads at a, the reactions, and the supports (wall, column or nothing).
  - `ColumnDiagram`: the levels it passes, the loads on its top, and its base connection.
- **Detail window:**
  - Opened from the selection card's **Detail** button (walls), or from the card of a selected beam or column (new).
  - It follows the selection and the design.
- **Report:** after the shear-wall pages:
  - "Level n: wall details" for each wall with uplift or forces from above.
  - "Beams and columns carrying hold-downs".
- **SWC:** From size… and Roof… are hidden. The building's size and roof are edited only in the Building Model editor.
- **Tests:** `DetailCalcsTests` (4), checked against the design formulas. SWC 184.
  - The three drawings were rendered to images and looked at.
  - The detail window and the report pages were not exercised; the apps were open, so SWC was built to a separate folder.

## 2026-11-02 — Columns follow their supports; point snaps; beam/column crossings (BuildingModel 9d3fa65, SWC)

- **One member.** A column is a single member from its base level to its top level.
- **Automatic base** (the default):
  - On every edit, a beam or wall drawn across its path trims it to stand there.
  - Deleting or moving its support runs it down again, to the next support or the foundation.
  - The status line says so, and Ctrl+Z undoes the edit and the trim together.
- **Base set by hand** (`Column.BaseFixed`):
  - Set from the right-click menu: **Run through to the foundation**, or **Stand on ▸ level**. The properties panel also has the checkbox and the Base list.
  - The column then stays as set: a post that runs through, or one that stops where the engineer says. **Base: stands on what is under it** returns it to automatic.
- **Warnings.** Selecting either one in the issue list selects both members and shows the level.
  - `BeamCrossesColumn`: a column runs past a beam through it, so they are not connected.
  - `BeamMissesColumn`: a beam passes within 1 ft of a column that reaches its level, but outside the 0.25 ft bearing tolerance.
- **Point snaps** (ranked first):
  - Column centres, including those of the story below.
  - Beam ends.
  - In SWC, the hold-down points of the level shown: from above, beam ends, column bases, the story's own, and the foundation anchors.
- **Tests:** 4 more in `BeamColumnTests`. Totals: BuildingModel 70, Sketch 57, SWC 180.

## 2026-11-02 — The hold-down load path: beams, columns, and what each hold-down type can anchor to

- **Building model** (BuildingModel dae5aff, 9174418):
  - **Beam (B):** two ends, in the framing of a level above the ground.
  - **Column (P):** one click. It runs down through the floors until a wall, beam or column carries it, or to the foundation, so it can be a multistory column (`BuildingEdits.ColumnBase`).
  - Both are saved in the model (file version 4), moved, copied, copied to another level, mirrored, deleted with their story, and checked.
  - Shown in plan (a brown dash-dot beam, a filled square column) and in 3D (boxes; a column stops under the beam it carries).
  - Both apps draw them: SWC uses the same editor, so one drawn there is saved to the model.
- **Core `HoldownRules`** (what a family can anchor to):
  - **STHD:** the foundation only (cast into concrete).
  - **Straps:** a wall or a column below, or wrapped around a beam; never the foundation.
  - **HDUE / DTT / HDU:** the foundation, or a rod into the post of a wall or column directly below; never a beam.
  - **LTT / HTT:** taken like HDUE (see "Needs your engineering confirmation").
- **Core `LoadPathTrace`**, run top down with the design. Each hold-down point goes to one of:
  - **A wall below.** A wall on the same line is already in that wall's design. A beam end or column base on a wall is added to that wall's forces from above, for both wind directions (conservative).
  - **A column.** The load goes down to the column's base, which is traced again; at the ground it is a foundation anchor.
  - **A beam.** Simple-span reactions, R_start = ΣT(L−a)/L and R_end = ΣT·a/L, per wind direction, with the worse one taken. Each beam end is a new hold-down point, sized for its reaction and traced to the wall or column under it.
  - **The foundation.**
  - **Nothing:** an error.
- **Sizing and flags:**
  - Where a wall's hold-down type cannot anchor to what is under it, the connection is sized as the type that can, and flagged (warning `HoldownTypeReplaced`).
  - New connections (beam ends, column bases) are sized from the project's catalog: a strap onto wood, the chosen anchored hold-down at the foundation.
- **SWC plan:**
  - Beams that carry hold-downs show their reactions.
  - Beam ends and column bases are brown squares with their reaction, support and model.
  - A replaced type shows in yellow.
  - The foundation view lists the column-base anchors too.
- **Tests:** `LoadPathTests` (4), `BeamColumnTests` (5), `View3D_ShowsBeamsAndColumns`. Totals: SWC 180, BuildingModel 70, Sketch 53.
- **Next:**
  - The beam/column detail when one is selected (loads, reactions, attachments).
  - The wall elevation view with its hold-downs and those from above, and the same drawing in the report.

## 2026-11-02 — Where the hold-downs land: on the wall below, on nothing, on the foundation (SWC)

- **Core** `StoryCascade.Landings`:
  - Each wall that needs hold-downs has two, on its post centerlines (`WallSchedule.HoldownPoints`).
  - Each one lands on a wall of the story below (same line, between its ends, within `LineTolerance`) or on nothing. For the first story, they land on the foundation.
  - `HoldownLanding` carries the wall, point, ASD tension, model and support.
- **On each level's plan:** the hold-downs of the story above.
  - A dark triangle where one lands on a wall, with its uplift (↓ 2.4k).
  - A red triangle reading "no wall below: beam or post" where nothing is under it. This is also an error in the issue list, on the level where the support is missing.
- **Foundation view** (the Foundation button before the level buttons):
  - The ground plan, with the first story's walls dashed and every hold-down anchor labelled with its wall, model and uplift. The caption gives the count and the largest uplift.
  - Picking a level returns to editing.
- **Tests:** `Landings_OnAWallBelow_OnNothing_OrOnTheFoundation`. SWC 176/176.
- **Next:**
  - Beams: a building-model object that can be drawn in either app. Hold-downs and straps from above load it, and its end reactions become hold-down points on what is under its ends.
  - Then the wall elevation view: the wall, its hold-downs, those coming down from above, and the calculations, with the same drawing in the report.

## 2026-11-02 — Diaphragm: rigid, flexible or the envelope of both (SWC)

- **Before:** every level was calculated as a rigid diaphragm. The flexible calculator existed and was tested, but the app never used it. The per-diaphragm `DiaphragmType` is still saved, but nothing reads it.
- **Project setting** `DesignSettings.Diaphragm` (Rigid / Flexible / Envelope):
  - It's chosen in **Diaphragm:** on the Design tab, and the tooltip explains each choice.
  - It's saved with the project's design settings. Older projects open as Rigid, the way they were calculated.
  - The program does not classify the diaphragm; the engineer chooses.
- **Envelope** (`ShearWallCalculator_Envelope`):
  - It runs both calculations on the same walls, diaphragm and loads, and each wall gets the larger shear.
  - The governing case is labelled, e.g. "Case 2 (flexible)", and `Governs` records which run gave each wall's shear.
  - The range (most + / most −) covers both runs.
  - Multi-story, the rigid run of a level carries the rigid reactions of the level above and the flexible run the flexible ones. This makes two whole-building analyses, enveloped wall by wall.
- **Switching kinds:** `DiaphragmAnalysis.Ensure` turns a level's calculator into the chosen kind and keeps its walls, diaphragms, loads and settings. This happens on opening (`ShearWallAdapter.ToCalculators`) and when the setting changes.
- **Calculations tab:**
  - With the envelope, the summary says how many walls each run governs.
  - The all-cases table adds Rigid V and Flexible V columns (the governing one in bold).
  - Every case of both runs can be picked: "Case (rigid)" or "Case (flexible)".
- **Report:** a rigid-vs-flexible table per wall, then both calculation sheets.
- **Tests:** `EnvelopeDiaphragmTests` (4), against the stand-alone rigid and flexible calculations:
  - wall by wall, the envelope takes the larger of the two;
  - switching kinds keeps the data;
  - two stories, each run carries its own reactions down;
  - the setting is saved in the model.
  - SWC 175/175.

## 2026-11-02 — Fail and close-to-limit colours made distinct (SWC)

- Fail was vermilion and close was amber, which looked nearly the same. Fail is now a dark crimson (stripe `#C8102E`) and close a light golden yellow (stripe `#F2C200`). They differ in hue and lightness, and fails stay dashed.
- The plan pens and the status-line error ink read the brushes in `Controls/Ui.cs` (one place for the colours).

## 2026-11-02 — Plans under the levels belong to the building model only (SWC + BuildingModel)

- **One plan system**, in `BuildingModel.Sketch`: `PlanImage` (DXF, PDF page, picture; scale, rotation, position, opacity, lock), drawn by the sketch, with snaps to CAD linework.
  - The level stores a small record (`Sketch.Plan`); the file is copied into the project's `plans` folder, so undo stays cheap and the folder carries its plans.
  - Older files convert on opening: SWC's `ShearWall.Image` (the picture inside the file is written to `plans`) and the first editor's `Sketch.Underlay`.
- **Editor (Plan ▾):** import DXF / PDF / picture; set scale, straighten, move, rotate 90°; opacity, lock, hide; **Move to floor**, **Copy to floor**, **Remove**. Every one is an undoable model edit.
- **SWC:** shows or hides the plan (this screen only) and zooms to it; Image ▾ links to the editor. Its own image code, import dialogs and PDF reader were removed (moved to the sketch).
- **Tests:** `PlanImageTests` (15) and `DxfReaderTests` (11) in BuildingModel.Sketch.Tests; SWC 171, Sketch 40, BuildingModel 70.

## 2026-11-02 — The discontinued HDU kept as its own hold-down family (SWC)

- **New family** `HoldownFamilies.HDU_Legacy_ToFoundation`, with the data in `NDSProvisions/SimpsonCatalog/HDU_Legacy_Manager.cs`. It covers HDU2/4/5/8/11/14-SDS2.5 with the C-C-2021 p. 53 values, and is used only when a wall is set to it:
  - "HDU (disc.)" in the wall table;
  - "HDU (discontinued)" in the catalog window, where its rows can be added to favorites;
  - the Source column reads "C-C-2021 p. 53 (discontinued, L-C-HDUECONV25)".
- **Source of the discontinuation:** Simpson letter L-C-HDUECONV25 (April 2, 2025), saved at `docs/references/L-C-HDUECONV25.pdf`. Its Tables 1 and 2 match the coded maximum loads and dimensions.
- **Citations:** the design-page footnote, the report footnote, and the catalog window and its printout cite C-C-2021 p. 53 and the letter for HDU.
- **Tests:** two new tests in `SimpsonCatalogTests`. SWC 191/191.

## 2026-11-02 — Hold-down catalog moved to Simpson C-C-2026; a copy kept in the repo (SWC)

- **Reference copy:** `docs/references/C-C-2026.pdf`, downloaded from Simpson Strong-Tie's catalog page (see `docs/references/README.md`).
- **Values updated to C-C-2026** (details in `DATA_VERIFICATION.md`):
  - The HDU-SDS2.5 line is replaced by HDUE3/5/7/9/13/17 (p. 61), and DTT1Z's minimum member goes back to 1½ × 3½.
  - HTTH6.5 is added and the HTT5 CL becomes 1-3/8 (p. 63).
  - MSTC66 takes 68 nails (p. 291).
  - STHD is unchanged (p. 67).
- **The edition is cited in the app:**
  - One source in the code: `SimpsonCatalog.Edition`, `Citation` and the `Pages*` constants, plus `ShearWallDesigner.CatalogSource` and `CatalogFootnote`.
  - The hold-down catalog window shows a source line under the title and a Source column ("C-C-2026 p. 61"), and both appear in the printout.
  - The Design tab has a footnote under the wall details, and the Hold-down¹ column refers to it. The report prints the same footnote.
- **UI names:** "HDUE" in the wall table; "HDUE / DTT" and "LTT / HTT / HTTH" in the catalog.
- **Tests:** `SimpsonCatalogTests` checks the whole HDUE/DTT table, HTTH6.5, every strap load and the edition citations. The design tests now expect HDUE models. SWC 189/189.

## 2026-11-02 — LSTHD / STHD hold-downs; the hold-down catalog checked against Simpson C-C-2021 (SWC)

- **New hold-down type: LSTHD / STHD** (`HoldownFamilies.STHD_ToFoundation`, `NDSProvisions/SimpsonCatalog/STHD_Manager.cs`). It covers LSTHD8, STHD10, STHD14 and their RJ models, with every value from C-C-2021 p. 59.
  - Each model has two options: `#1` is the 6" stemwall, `#2` the 8" stemwall. Both can be added to the standard list (favorites) in the catalog window.
  - The allowable load and the nail count come from the project's STHD conditions: stemwall, cracked or uncracked concrete, midwall / corner / endwall, and the seismic row. Set them with the Design tab's "STHD: …" button. The defaults are 6", cracked, endwall, Wind and SDC A&B.
  - The wall table offers "STHD"; the wall details and the report state the conditions. The catalog window has an STHD family filter and a Conditions column, and follows changes to the conditions.
- **Catalog checked** against C-C-2021 pp. 53, 55 and 269. Five corrections, listed in `DATA_VERIFICATION.md`: MSTI36 length, MSTC52 SPF/HF load, LSTI code refs, DTT1Z minimum member, LTTP2 deflections. Every dictionary entry now cites its edition, page and table.
- **Tests:** `SimpsonCatalogTests` (6). SWC 186/186.

## 2026-11-02 — One project file, in a project folder; the model's geometry drives the wind (SWC + BuildingModel + W7)

_Branch `dev/ui-pass-2` in all three repositories._

- **The project is the building model file.** `<project folder>\<folder name>.bldg.json` is the only project file of all three apps. The old `*.swc.json` format (`Interfaces/ProjectFile.cs`, `ProjectSerializer`, `WindInputs`, `WindBuildingInputs`) is gone, without migration (no files in use).
- **Who owns which part of the file:**
  - The building: levels, walls, diaphragms, openings, roof and defaults. Only the Building Model editor changes the levels, size and roof. SWC draws walls and diaphragms into it, and the wall design goes in their bags.
  - `"Wind"` (`ASCE7Wind.ModelAdapter.WindSection`): the site, enclosure, components, edition and cases. It is written only when the project takes the wind loads.
  - `"ShearWall.Project"` (`ShearWallCalculator.ModelAdapter.ShearWallProject`): manual loads per level, and the loads at the last save (you are told on opening when they differ).
  - `"ShearWall.Image"` on each level: that level's plan image.
- **Save and open:**
  - At save, these sections go into a copy of the building (`ShearWallProject.Save`). On opening they are taken out of the building being edited (`ShearWallProject.Open`), so they are not in the undo history.
  - A model that has no SWC section yet (made in the editor) opens as a new design that takes the wind loads.
- **Project folder** (`BuildingModel.Diagnostics.ProjectFolder`, `BuildingModel.Sketch.ProjectDialogs`):
  - New project asks for a folder, then the building's size (the editor's New from size). The model is saved there at once.
  - Logs go in `<project>\logs`: `bldg-*.log`, `swc-*.log`, one file per app per day. Every file dialog starts in the project folder.
- **Nothing without a project:**
  - At start, SWC shows a start screen: New project, Open, recent projects, Exit. A `.bldg.json` given on the command line opens directly.
  - While the model has errors (ModelChecks), the design and the report are blocked and the reason is shown.
- **Wind geometry from the model:**
  - Wind Setup shows L, B, the stories, plate heights, roof, overhang and parapet from the model, read-only (`WindSetupControl.SetModelGeometry`). Its "Edit building…" button opens the editor.
  - The enclosure, parapet envelope and components are still set there.
  - The retired BuildingData and WIND input tabs and the floors-from-wind code (`MatchLevelsToStories`, `FloorsToBuildingData`) are removed.
- **Manual loads:** used only in a project without wind inputs. Entering one in a wind project asks first; the wind section is then left out at save.
- **Model > Edit building in the Building Model editor:**
  - It saves the project and starts `BuildingModel.exe` on it; the build copies the exe next to the app.
  - SWC waits behind a notice until the editor closes, and reloads the project after each save made there.
- **Logging:**
  - One logger for the three apps (`BuildingModel.Diagnostics.Log`).
  - Logged: every sketch edit, undo and redo; model opens and saves, with revision, counts and sections; failed opens and saves; wind inputs and loads; new, open, save and reload of projects; reports; crashes.
- **Tests:**
  - SWC 180. New: `ShearWallProjectTests`. The old round trips now go through `TestProject.RoundTrip`, the real save and open path.
  - BuildingModel 70. New: `ProjectFolderTests`.
  - W7 278 + 2 opt-in.
  - W7 UI 57 of 58. New: `ModelGeometry_IsShownNotEdited_EnclosureStaysHere`. `EveryStep_FitsWithoutScrolling (70)` already failed before these changes.

## 2026-11-01 — The plan is drawn in the shared sketcher; the building model is the source of truth (SWC + BuildingModel)

_Branch `dev/shared-sketcher`. Details: `SHARED_SKETCHER.md`._

- **Drawing:** the shared sketcher (`F:ProgrammingBuildingModel`, `BuildingModel.Sketch`) replaces the old canvas and its DRAW / EDIT / snap buttons. New: openings between levels, notes on the plan, mirror, split and join walls, grips to stretch, window selection. L, R and S still work; debug drawing is F12.
- **Model:** walls, diaphragms, openings and notes live in a building model (`BuildingModel.Core`); the calculators (one per story) are filled from it after every change (`ShearWallCalculator.ModelAdapter`, in this repo). Design inputs are stored in the model, so one undo covers drawing, design and floors (+ Floor, ✕ Floor and Copy ↑ can now be undone).
- **Errors on the plan:** model checks, failing walls and stories that cannot be calculated are marked on the plan (red / amber) and listed under the issues button.
- **Model menu:** 3D view, check the model, open / save a `.bldg.json`, export and apply change sets.
- **Project file:** an optional `Model` field; older files open and are converted.
- **Tests:** SWC 162; BuildingModel 49 + 11 sketch tests.

## 2026-11-01 — Multi-story, 1 to 3 floors: wind per floor, analysed from the roof down (SWC + W7)

- **Floors** in the building inputs (Wind setup, next to the size): 1, 2 or 3 (3 is the maximum for now). The story height is the eave height ÷ floors (shown beside it). Each floor is a **level**: the walls of that story and the diaphragm over them; the top level is the roof.
- **Wind per level (W7 core `StoryWindLoads`):** the walls are cut into strips from mid-story to mid-story. Level 1 runs from the ground (conservative: the levels add up to the building base shear exactly, and one floor gives the same loads as before). The roof strip runs to the top and also takes the roof and parapet forces. Chapter 27: windward qz(z) integrated over each strip, leeward and sides uniform; Fig. 27.3-8 cases are rebuilt per level with that level's forces and the building's 0.15 B. Chapter 28: each case split by the level's share. The 27.1.5 minimum is 16 psf on each strip's walls (8 psf roof at the roof) when it governs. The Wind setup link message lists the force on each level.
- **Analysis from the top down (core `StoryCascade`):** the roof level is calculated first. Each of its walls then pushes on the floor below with minus its shear at the wall's middle; the floor adds those loads to its own wind, case by case (by name), at its center of mass with their moment about it. So a wall that stacks passes its shear straight down, and one that does not is spread by the floor below (the real load path). With a manual load on a level, that load goes with every case from above. A flexible floor takes the loads from above as a resultant plus torsion (flagged as an approximation; point loads by statics are the next step).
- **Checks shown on the Calculations tab:** a wall above with no wall under (most of) it is listed ("needs a post, beam or blocking under its hold-downs"); each case line shows the part that comes from the level above (Vx, Vy, Mt about the CM).
- **Drawing:** LEVEL buttons (1, 2, 3 Roof) at the left of the toolbar appear when there is more than one floor. The level below shows dashed gray under the current one, and its walls snap, so walls can be stacked. **Copy ↑** copies every wall (with its design) and diaphragm of the level below onto the current level to start it (Ctrl+Z undoes it). Each level keeps its own undo history and its own plan image. Lowering the number of floors asks before deleting a level that has walls.
- **Design:** the levels share one set of design settings; the Design tab and the plan colors show the level being edited. The printed report has every level, roof first, each with the loads it takes from above.
- **Project file format 3:** the levels above 1 are saved as `UpperLevels`, the number of floors with the wind inputs. Older files open as one floor.
- **Toolbar:** Walls and Diaphragms are now separate SHOW toggles (was one "Walls & diaphragms" button).
- **Tests:** SWC 150 (`StoryCascadeTests`, 10 new, by hand: stacked boxes, roof 10 k + floor 8 k gives 9 k in each lower wall; the reactions of uneven upper walls have exactly the upper case torsion about the CM; a wall moved in below is flagged and the floor's eccentricity is −5 ft; manual load below with two wind cases above; supported length as a union of walls on the same line; copy floor; one level unchanged; two-story wind end to end: level 1's walls carry the building base shear in every case; the project file keeps the upper levels; an empty or unfinished level above leaves level 1 as before). W7 271 (7 new, `StoryWindTests`: strips; one story = the building loads in both chapters; two stories by hand integration of qz; gable roof force at the roof; the minimum by strip; Chapter 28 split by area).

## 2026-11-01 — Tables: delete and select on every tab, click-to-sort columns; the drawing fills the height (SWC)

- **Delete from any table:** the × at the end of each wall row on Walls & diaphragms, Calculations (envelope, each load case, flexible walls) and Design deletes that wall (diaphragms on Walls & diaphragms). All go through the same delete as the drawing, so Ctrl+Z brings it back.
- **Select from any table:** clicking a row on any of the three tabs highlights that wall or diaphragm on the drawing with a wide amber glow (stronger than before), and pans the view to it when it is off screen. The Design tab follows the wall picked elsewhere.
- **Sorting:** every table on the three tabs sorts by clicking a column header (again: reverse; a third time: back to the wall order); ▲ / ▼ shows the column. Numbers sort as numbers ("3 (E-W)" by 3, 6" by 6), blanks last; the sort stays while the tables refresh. `GridSorter` in `Controls/Ui.cs`.
- **Drawing area:** it now fills the window height under the toolbar (it was a fixed 750 × 600 with empty space below). Drag the bar between the drawing and the tabs to make it wider or narrower; the view keeps its place when the size changes. The details card sits at the drawing's top-right corner whatever its width.
- **Tests:** SWC 140 (`DrawingEdits.Center` added to the drafting test). The three tabs rendered off-screen with a six-wall building: sorted by Length, Design V and Ratio, × on every row.

## 2026-11-01 — Drafting: undo / redo, delete, move, copy, measure, ortho, fit, pan (SWC)

- **Undo / Redo** (Ctrl+Z, Ctrl+Y or Ctrl+Shift+Z; ↶ ↷ on the toolbar; Edit menu). Covers walls and diaphragms added, deleted, moved, copied or resized (drawing or tables), each wall's design inputs and the project design settings; up to 200 steps. The status line says what was undone ("Undone: wall 3 added"). Loads, wind inputs and the plan image are not part of it. A new or opened project starts a fresh history. In a text box Ctrl+Z keeps undoing the typing. Core: `Drawing/DrawingHistory.cs` (snapshot of the walls and diaphragms by id after every update).
- **Delete** key (or Edit > Delete, or the card's Delete) removes the selected wall or diaphragm without a question; Ctrl+Z brings it back.
- **Move (M) / Copy (C):** pick the wall or diaphragm (or select it first), click a base point, click the new place: snaps work, or type `@dx,dy`. Move keeps the id and design; Copy makes a new wall with the same height, type and design (its own copy), and stays on for more copies from the same base point until Esc. A dashed outline follows the cursor. Core: `Drawing/DrawingEdits.cs`.
- **Measure (T):** two points (snaps work); the line and its length stay on the plan, and the status line shows the distance, Δx and Δy.
- **Ortho (F8):** move, copy and measure go straight across or straight up. Walls are always square (unchanged).
- **Walls chain:** after a wall, the next one starts at its end (like a CAD line command); Esc or a right-click ends the chain.
- **Fit (Z):** zooms to the walls, diaphragms and the shown plan (it used to reset to a fixed 120 ft view). The wheel zooms at the cursor (as before); **drag with the middle button to pan; double-click it to fit.**
- **Space or Enter** in Select repeats the last tool.
- **Tests:** SWC 140 (6 new, `DrawingEditTests`): undo / redo of add, design edit and delete, with the steps named, redo dropped by a new change; undo restores the centre of rigidity exactly; the 200-step limit; move keeps id and design and updates centroid and centre of mass; copy gets a new id and its own design; delete, extents and ortho.

## 2026-11-01 — Plan import: DXF (true size) and PDF sheets; old image code removed (SWC)

- **One command for every plan:** Edit > Import plan (DXF, PDF, image)..., or Image ▾. The file type picks the dialog. A DWG gets a message: save it as DXF in AutoCAD.
- **DXF** (`Drawing/DxfReader.cs`, written here, no library: DXF is Autodesk's published text format). Reads LINE, LWPOLYLINE and POLYLINE (with arc bulges), ARC, CIRCLE, ELLIPSE, SPLINE (through its fit or control points) and block INSERTs (nested, scaled, rotated, arrays; layer-0 entities take the insert's layer); mirrored entities (extrusion −Z) are flipped. Model space only; text, dimensions and hatches are skipped and counted. Units from $INSUNITS; when the file does not say, the dialog asks (inches by default) and shows the plan size so a wrong unit shows at once. **Layers:** tick the ones to keep. The linework comes in at true size, is drawn as crisp lines at any zoom, and its ends, midpoints and crossings are object snaps while drawing walls (only the lines near the cursor are checked).
- **PDF** (`Helpers/PdfPages.cs`): rendered by the PDF engine built into Windows 10/11 (Windows.Data.Pdf, referenced from System32\WinMetadata, part of every Windows 10/11 machine; no SDK, no package). Pick the sheet and the printed drawing scale: the paper size is in the PDF, so there is no resolution to guess (rendered at up to 200 dpi, longest side at most 6000 pixels). "Not to scale" sets the scale on the drawing with two points. The rendered page is stored in the project.
- **Pictures** (PNG, JPG, TIFF...): as before.
- **Removed (user: clean up the former image importer):** `ImageLayer.cs`, the image members of `LayerManager`, `EnterMeasurementDialog`, the older-project fields `selectedImageFilePath` / `pixelScaleX` / `pixelScaleY` and their migration. Older project files still open; a plan image saved the old way is not brought back (import it again).
- **Tests:** SWC 134 (10 new, `DxfReaderTests`, by hand: units; line, polyline, arc, circle; bulge half circle; old-style polyline; block insert scaled, rotated and moved with layer 0 taking the insert's layer; mirrored arc vs line; paper space, text, dimensions and hatches skipped; DWG / non-DXF refused; true-size placement, endpoint snap, project round trip). PDF checked by rendering a hand-made two-page PDF: 8.5 × 11 and 36 × 24 in read back, lines at the right pixels.

## 2026-11-01 — Plan image stored in the project file (SWC)

- **The image file itself is saved inside the project** (`ReferenceImage.Data`, base64 in the JSON), with its final scale, rotation, position, opacity, lock and on/off state. Reopening the project brings the image back at its scale even if the original file was moved or deleted, or the project is opened on another computer. The project file grows by about 1.3 × the image file size.
- Older projects that saved only the path: the image is loaded from the path (or next to the project file) once and stored, so the next save carries it.
- **Layer:** the image is drawn under everything and is never selected by clicks; only the Image ▾ commands change it. Toggle with the toolbar **Image** button or Image ▾ > Hide/Show image layer; the on/off state is saved with the project.
- Log: "stored in project: yes, N kB" is part of the image record. Test: save → delete the original file → reopen keeps the bytes, the calibrated scale, rotation, origin and the hidden state.

## 2026-11-01 — Two-face sheathing: faces are never added (SWC)

- **User decision (after their own research):** two different materials on opposite faces take the larger of the stronger face and 2 × the weaker face (SDPWS 2021 4.3.5.4.2; 2015 4.3.3.3.2); the same construction on both faces is 2 × one face (4.3.5.4.1). The wind exception that adds a wood structural panel face to a gypsum wallboard face is **no longer used**, because a simple sum can be unconservative. max(S, 2W) ≤ S + W always.
- **Removed:** the "Add wood + gypsum faces (wind)" setting (`DesignSettings.SumWindFaces`) and `SheathingRules.MaySum`. Older project files that hold the setting open; it is ignored.
- **In the app:** the settings bar shows "Two faces: max(stronger, 2 × weaker) ⓘ" with the rule, the reason and the sources as a tooltip; Help > Two-face sheathing rule... shows the same text; the report's design section prints it (`SheathingRules.TwoFaceRuleNote`). Each wall's steps already name the rule used.
- **Tests:** summing tests replaced: wood + gypsum gives max(785, 2 × 200) = 785; the combined value is never more than the sum; an older project with the setting on still gives 785.

## 2026-11-01 — Plan image importer replaced; the log records every model change (SWC)

- **Plan image (replaces Edit > Open Image Tool and its separate measuring window).** File: Edit > Import plan image..., or Image ▾ on the toolbar.
  - **Import:** pick the file. The scale comes from the printed drawing scale (1/16" … 1/2" = 1'-0", 1" = 10' … 40') and the resolution, read from the file when it has one; the dialog shows the building size that gives, so a wrong dpi shows at once. Without a resolution (screenshots, photos) the default is to set the scale on the drawing. The image goes in with its lower-left corner at (0, 0) and the view zooms to it.
  - **On the drawing** (Image ▾), with prompts on the status line, Esc to cancel: *Set scale* — click the two ends of a known dimension, type the real length in the Point box (24'-6", 24.5); the image rescales about the first point. *Straighten* — two points on a wall line; the image turns so it runs exactly E-W or N-S. *Move* — base point, then destination (snaps to the walls). *Rotate 90°*, *Opacity* (25–100 %), *Lock*, *Zoom to image*, *Remove*.
  - The image is drawn with its scale, rotation and position (any angle), saved with the project (path, scale, rotation, position, opacity, lock, shown). A project moved with its image finds the image next to the project file. Older projects (path + one scale) open as before.
  - Core: `Drawing/ReferenceImage.cs` (pixel ↔ world, calibrate, straighten, rotate, move, drawing scale). App: `Controls/ImageTool.cs`, `Dialogs/ImportImageDialog.cs`.
- **Log:** every change to the model is logged with before → after, whichever panel made it: walls and diaphragms added, deleted, moved or resized; each wall's face 1 and face 2 sheathing and edge nailing, framing, dead load, hold-down type and face; design settings (method, edition, hold-down pool, sheathing for new walls); the loads; the plan image (added, scale, rotation, position, opacity, lock, shown, removed). Also New, Open and Save. (`Diagnostics/ModelChangeLog.cs`: a snapshot of the model in words, compared after each change.)
- **Tests:** SWC 124 (9 image: import placement, drawing scale, two-point scale, straighten, rotated round trip and drawing matrix, move, older projects, project file, moved project; 4 log: nothing logged without a change, walls, sheathing/nailing/framing/dead load/hold-down, diaphragm/settings/loads/image).

## 2026-11-01 — Hold-down lever arm d = L − post (SWC)

- **Hold-down tension** is now T = 0.6 M / d − 0.6 D L / 2 with d = L − post: the hold-downs sit on the post centerlines, half a post in from each end (user). The dead load acts at L/2, which is d/2 from the compression post, so its share stays D/2. The post is the hold-down's first minimum member size (e.g. HDU2 3 × 3.5 → 3 in; DTT2Z with 1.5 × 3.5 → a single 2x). The post depends on the hold-down chosen, so the check starts with two 2x studs (3 in) and repeats until the choice settles. The details show "d = L − post = ...".
- Hand check (test): L = 10 ft, h = 9 ft, V = 5 k: d = 10 − 3/12 = 9.75 ft, T = 27000 / 9.75 = 2769 lb (was 2700), HDU2-SDS2.5 (3075 lb).
- **Two-face rule, sources** (user asked): different materials on opposite faces take the larger of the stronger face and 2 × the weaker; wind exception: WSP, hardboard siding or structural fiberboard + gypsum wallboard are summed. SDPWS 2021 §4.3.5.4.2 and its exception (SDPWS 2015 §4.3.3.3.2); same construction both faces 2 × one face, SDPWS 2021 §4.3.5.4.1. Secondary sources checked: AWC FAQ "Is summation of capacities allowed for shear walls sheathed on both sides with different materials?" (2021-09-27) and WoodWorks "Shear walls with multiple layers of shear resistance" (SDPWS 2021). max(S, 2W) ≤ S + W always (W ≤ S), so the rule is never larger than a simple sum: the sum is the less conservative value and is allowed only by the wind exception. The printed text still needs checking: the AWC FAQ names only WSP + gypsum for the exception, WoodWorks also hardboard and fiberboard.
- **Tests:** SWC 111 (tension tests now at d = L − post).

## 2026-10-31 (late night) — The design on the drawing: schedule marks, hold-downs, schedule (SWC)

- **Schedule marks:** each different sheathing construction gets a mark, SW1, SW2, ... from the weakest (lowest design capacity) up, as on a drawing set. Each wall shows its mark and hold-down model in a small tag ("SW2 | DTT2Z"), outlined green (passes) or red; "no HD" in red when no hold-down is strong enough. The tag sits above an E-W wall or left of a N-S wall, on the other side if that would leave the canvas.
- **Hold-downs** at both wall ends (wind reverses), on the post centerline: inset from the wall end by half the post dimension along the wall (the first of Simpson's minimum member sizes; two 2x studs, 3", when none is given). Filled square; open red square when none fits.
- **Schedule** on the canvas: mark, sheathing, capacity, walls, and the hold-down symbol; placed in the corner that covers the least of the drawing.
- SHOW **Design** on the toolbar turns it on and off (on by default). Walls stay green/red by pass/fail.
- **Tests:** SWC 111 (4 new: marks per construction weakest first, empty design, hold-down points on both ends of E-W and N-S walls, post size along the wall).

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
- **Not covered → falls back to Ch. 27 with a note:** h > 60 ft, h > least plan dimension, open buildings. (Hip roofs are now covered, per Fig. C28.3-3; see the 2026-11-04 wind entry at the end.)
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
11. **Hold-down families, floor to floor** (`HoldownRules`). LTT / HTT is taken like HDUE: the foundation,
    or a rod into a post directly below. Confirm the catalog's floor-to-floor installations for LTT / HTT.
    Beam ends and column bases on a wall add their reaction to that wall's overturning for both wind
    directions, which is conservative.

## Next

- **M2 remainder:** typed zone identifiers in place of `"Zone2e"` strings, and one result list in place
  of 13 dictionaries. This needs coordinated UI changes.
- **M3:** a single `BuildingModel` with 3D surfaces and local frames (the zones are already per-surface
  polygons).
- **M4:** the `Viewport3D` viewer with mouse orbit, pan and zoom, and pressure-colored zones.
- The workspace loads line names the level: "Level 3: its own X 8.2 k, Y 13.6 k (whole building X 31.4 k, Y 51.9 k)";
  a lower level adds what its walls take with the levels above (the bottom story: the whole building's base shear). The
  toolbar says "Wind loads (auto) on this level". Each level's wind is its strip of the building, so the level shown is
  less than the wind calculator's total; nothing was lost (Level 2's walls: 23.2 + 8.2 = 31.4 k).
- The loads sentence is now a small table beside the workspace buttons, roof first: each level's "Wind on the level"
  (its strip), "Its walls carry" (its strip plus every level above), and "Foundation (total)" (the base shear, the wind
  calculator's total). The level shown, or the foundation, is highlighted. The Calculations card says "Story shear
  (walls resist)" (Level 2 of junk: Vx 31.4, Vy 51.9 k), not the level's own strip; the analysis always used the story
  shear (StoryCascade.WithLoadsFromAbove: W2 + W4 = 15.72 + 15.72 = 31.4 k). The toolbar shows the base shear on the
  foundation.
- Objects drawn twice are flagged (BuildingModel ModelChecks, warnings): ColumnsOverlap (two columns within the bearing
  tolerance over a shared height: junk's C3 / C5), BeamsOverlap (one level, one line, overlapping more than end to end),
  WallsOverlap (a wall that is not a shear wall on another wall; two shear walls stay the WallOverlap error).
  Test: EditsTests.Checks_ObjectsDrawnTwice_AreFlagged.
- Hold-down labels have leaders: a thin line from the point to the label's nearest edge with a dot, so a label moved
  clear of others (it moves as the zoom changes) still shows its point. Leaders are drawn first and the labels over them,
  so no leader crosses text. On the foundation, the anchors at one point share one label (a line each); by the plan's
  right edge a label goes wholly to the left of its own point, not beside the next point.
- The Diaphragm choice (Envelope / Rigid / Flexible) moved to the top of the Calculations tab, where its results show,
  with a line saying what it does; the Design tab names the one in use ("Envelope (rigid and flexible) (set on
  Calculations)"). Envelope is the default for a new project (DesignSettings.Diaphragm); a project keeps the one it saved.
- Level notes that are information (an approximation of the method, StoryCascade.InfoMark) show as a blue note,
  "Note (information, not an error): ...", not a yellow warning: the flexible run spreads the walls above over the floor
  as a uniform load plus their torsion, not at each wall.
- Schedule marks are unique across the project and name their plan: SW{level}-{n}, numbered weakest first on each level
  (SW2-1 is mark 1 on the Level 2 plan). The level part is the number ending the level's name; a level named otherwise
  gets its initial (Roof: SWR-1), not its story number, which could be another level's. Test:
  WallScheduleTests.Schedule_MarksNameTheirLevel_UniqueAcrossTheProject.
- The Design tab has a Mark column (after Wall): each wall's schedule mark, as tagged on the plan (SW2-1). One rule for
  both: WallSchedule.Build(results, WallSchedule.LevelTag(level name, story)). A mark is a construction several walls
  share, so it is a column beside Level and Wall, not in their place.
- Correction: a mark is one wall's own design, made of its level and its number: SW{level}-{wall} (wall 9 on the
  Level 2 plan is SW2-9). Two walls with the same sheathing keep their own marks. The drawing key lists each mark with
  its sheathing, capacity and hold-down. WallSchedule.Mark / Build; tests Schedule_OneMarkPerWall_ItsLevelAndNumber,
  Schedule_LevelTag_FromTheLevelName.
- What works on what: the plan has its own strip across the top of the viewport, LEVEL (G, 2, 3: which plan is shown,
  the short names as in the marks) and SHOW (Image ▾, Design, Loads ▾, 3D, issues); the top toolbar keeps the project's
  Floors… and Loads. The Design tab's banner says what its Floors filter lists ("ALL FLOORS · 10 walls · Floors picks
  what this table lists") and its rows no longer mark the plan's level: the level buttons change the view only.
- Foundation: what the walls meeting at a point put on it together, case by case (FoundationForces). Each first-story
  wall's ends per case (EndForceByCase, + up / − down, ASD): the couple 0.6 V h / d (+ the walls above, taken equal at
  the compression end) less the dead load, with 0.6D for uplift (T) and 1.0D for bearing (C). At each point the sum in
  each case, the worst T and the worst C (in their own cases). Junk's W1 + W2 corner: T 6,854 lb together, not 4,655 +
  3,999. Column bases add their uplift; their gravity is not calculated (no C). Labels short: "Σ T 6,854  C 6,854";
  caption "max at a point: T … C … lb ASD". Tests: EndForces_ByHand_UpliftAndBearing,
  Foundation_WallsMeetingAtAPoint_AddCaseByCase_UpliftAndBearingSeparately.
- Foundation reactions are their own overlay (SHOW Reactions, shown on G only, on by default): one label per point,
  "+6.9k (C) / −6.9k (T)" (+ compression, − tension, kips ASD), below right with a leader; the Design overlay keeps the
  hold-downs only. A column-only point shows its T only (its gravity is not calculated). Caption in the same format.

### Viewport "what's shown" label
- The end of the viewport strip says what the plan shows: "Wall design · Wind loads" on a level, "Wind only: hold-downs, reactions" on G.
- The foundation caption reads "Foundation (wind only)", so the reactions aren't taken as everything the foundation carries (gravity isn't included). The strip wraps instead of clipping.

### PLAN / DATA flyouts
- A tab on each edge of the Shear walls workspace: PLAN (left) closes or opens the plan, DATA (right) the tabs. Either side can take the whole width, or both stay open as before.
- Both can't be closed at once (closing one reopens the other). Reopening restores the splitter width.
- The Wind loads workspace has no flyouts. Code: MainWindow.Panels.cs.
- The black caption on the plan is gone on every level. The level buttons and the "what's shown" badge (now filled purple) say the same thing. The foundation's max values stay in the status line.
- The COM/COR coordinate text in the plan's corner is gone too. The markers stay on the plan, and the coordinates are on the Calculations card.
- The badge uses 1–2 words: on G it's "Wind only", "Wind hold-downs" or "Wind reactions". The strip is one row, so the badge trims instead of wrapping.
- Zoom to the drawing on G leaves room for the anchor and reaction labels. SWC measures how far the labels reach past the drawing on each frame and passes that to the sketch (SketchCanvas.FitPadding). Level plans fit as before.
- The scale bar moved to the plan's bottom-right corner, one row above the grid's numbers, so it no longer overlaps them or the line tags along the bottom left (BuildingModel.Sketch).

### Wind loads workspace: results only on the Results page
- Detailed calculations, 3D Wind and the "Wind loads (auto)" table at the top are hidden on Site and Building. They appear on the Results page, after the inputs have been gone through. Going back hides them again.
- The Shear walls workspace always shows the loads table, because those are the loads its walls are using.
- Driven by WindSetupControl.StepChanged / OnResults (ASCE7 repo). MainWindow.WindStepChanged applies it.
- Wind Setup page 2 (Building) shows the building only: the model's box, roof, floors and north mark, redrawn as the numbers change. There are no pressures, legend, wind modes, wind arrow, windward/leeward labels or result cards. Those are on Results. Wind3DViewerControl.GeometryOnly (ASCE7 repo).
- When the wind inputs can't be calculated, the building is still drawn (no pressures) instead of an empty view.
- With a building model, Wind Setup page 2 shows the model's geometry (size, floors and plate heights, roof, eave overhang, parapet) as a read-out under "From the building model", with Edit building…. The locked input boxes and roof buttons are gone. The wind's own inputs (parapet envelope, cladding components) stay editable.
- The Building 3D view (the 3D button) draws the roof: gable or hip from the roof plan, widened by the eaves, up to the ridge height, or a flat roof's parapet. Its fit frames the whole building whatever the window's shape. Tests by hand: a 40 × 20 ft plan at 45° gives a ridge at 20 + 10 = 30 ft (BuildingModel.Sketch).
- Wind Setup page 2 shows that same 3D view of the building model (WindSetupControl.BuildingPreview). The wind library's plain building view stays as the fallback when there is no model.

### Issues button on every workspace; wall tags along their walls
- The issues button sits in the top bar, so it shows on both workspaces and every tab. Solid red "✗ n errors" when there are errors (it flashes a few times when the count goes up), amber "⚠ n warnings" with warnings only, green "✓ No issues" when clear. The list popup has its own dark text (a popup inherits the button's white).
- Clicking an issue in the list goes to it from anywhere: Shear walls workspace, PLAN opened, its level shown, the members selected (the selection card shows why they fail), and the view zoomed to frame them at least 25 ft across. The popup closes. SketchCanvas.ShowIssue / IssueBounds, IssueList.Picked (BuildingModel.Sketch, tests in IssueZoomTests).
- Design tags (SW2-1 | HDU…) on N-S walls are turned 90° to run along the wall, read from the right; E-W walls are unchanged (DesignOverlay).
- Pass is green everywhere (Ui.PassInk #1E7B34, PassFill #E6F4EA, PassStripe #2E9E4F): passing walls on the plan (they were the plain shear-wall blue), the SW tag outlines, ✓ checks and "n of n walls pass" on the Design tab, the issues button's "✓ No issues", and the "Wind loads (auto)" header. Close stays yellow, fail crimson and dashed; the ✓ ▲ ✗ marks keep the states apart without colour.

### Hold-down size chosen by the engineer
- The Design table's Hold-down column is a box per wall: "Auto · HDUE7-SDS3" (the lightest that works, as before) or any model of the wall's hold-down type, lightest first, each with its allowable load ("HDUE9-SDS3.5 · 8425 lb · min. 3.5 × 3.5"); models weaker than T are marked "✗ < T". A model listed more than once is told apart by its minimum post, and its fasteners when the post is the same.
- Saved with the wall (WallDesignInputs.HoldownKey, the catalog option key "Model#n"; null = Auto). The chosen model is used even when it is weaker than T (the wall then fails: "hold-down DTT2Z 1825 lb < T 2769 lb") and even when not on the company standard list. Its post sets the arm d, as for an auto pick.
- Changing the hold-down type goes back to Auto; a key not of the wall's type is ignored (Auto used, the tooltip says so). "Use for new exterior/interior walls" does not copy the chosen model.
- The detail line says "(engineer's choice)". HoldownOk now also checks the allowable against T. Tests by hand in HoldownChoiceTests (L 10 ft, h 9 ft, V 5 k: T = 27000 / d).
- The plan's wall card has the same hold-down box (Controls.HoldownPicks: one list for the table and the card), with "T = … lb · allowable … lb" under it. A pick there is the wall's design input as from the table: recalculated, saved, undoable, logged "(from the plan card)".
- Fixed: the schedule beside the plan did not refresh when only a wall's hold-down changed (its change check left the hold-down out).
- The plan's wall card also edits the wall's construction: Face 1 (sheathing and edge nailing), Face 2 (None, Same as face 1, or its own sheathing and nailing: one or two layers) and the Hold-down type, beside the hold-down size. The card and the Design table make the same edits (Design.WallDesignEdits, tests in WallDesignEditsTests: 785 plf at 6", 1205 at 4", two equal faces add); a card edit is applied, saved, undone and logged like a table edit.
- Wall tag placement fixed: a tag beside a column at its wall's middle stayed off the middle (the column's reserved box reached past the square, and touching edges counted as covering). Tags now slide in quarter-tag steps and never past their wall's ends (SW2-9 ran below BWL1). A tag or label with no clear spot takes the spot covering least, not its first try (a beam label lay across SW2-11). A Rect FirstOrDefault fallback that never fired (default is a zero box, not Rect.Empty) is gone.
- Windows fit the screen. The Detailed wind calculations window asked for 1400 × 900 on a 1920 × 1080 screen at 125% (work area about 1536 × 816), so it opened centred with its title bar above the screen and could not be closed. Every window and dialog now goes through one helper, BuildingModel.Sketch.WindowFit: at most 95% of the work area, the minimum size capped too, and on opening it is moved back on screen, title bar first. That covers the SWC main window's restore size, Detail, Hold-down catalog, 3D Wind View, Building 3D, Loads and STHD conditions, the editor's main window and its dialogs, and in the wind library the Detailed calculations and Report preview windows (WindDetailsWindow.FitSize/KeepOnScreen; it does not reference BuildingModel). Checked in the app: every window opens inside the work area with its title bar visible (Detail and the editor's main window used to be 1025 px tall in a 1020 px area). Tests: WindowFitTests, WindDetailsWindowTests. The fit uses the primary monitor's work area.
- Hold-down landing labels on a level plan are shorter and sit outside the building. A beam end now reads "BM2 ↑ 2.7k MSTI26": "end"/"start" are gone (the leader shows which end) and so is "on column C3" (the column is labelled at the point). Each label goes just past the building side nearest its point, or the next side when it would run off the plan (Drawing.PlanLabels.OutsideSpot). An arrow-tipped leader points back to the point. When the label is not past the point's own side, the leader steps out past that side first, so it never lies along a wall or beam on the outline (PlanLabels.Leader). The labels keep off the wall-line names (BWL), the grid numbers and the scale bar (SketchCanvas.ScaleBarArea), and Zoom to the drawing leaves room for them on level plans too. Tests: PlanLabelsTests.
- Loads overlay: each arrow and its label are one piece. The label sits right beside the arrow's middle, on the far side from the wall line, and the two slide along the line together to a clear spot (none clear: the spot covering least). Before, the label was placed by the general label routine, which saw it touching its own arrow and moved it away on its own (Level 2: "W2 top 18.69 k" well above its arrow, "W6 6.96 k" under another).
- Loads overlay legend on the plan: a short arrow in each colour with what it means. Teal: "Wall top: the floor's push on this level's wall". Orange: "From the wall above (Level 3) into this floor" (only when something comes from above). It goes in the plan's clearest corner and is reserved before the arrows are placed, so they keep off it. The key beside the plan shows only while something is picked; this one is always there with Loads on.
- A click on a wall's design tag on the plan (SW2-2 HDUE5-SDS3) picks the wall and opens its card, as a click on the wall does (Shift/Ctrl adds it to the selection). The design overlay hands back where it drew each tag that frame (DesignOverlay.Draw tagBoxes); the sketch's InterceptClick checks them before its own pick.
- A row picked in the Design table (or any table that selects on the plan) opens that wall's card on the plan, as a click on the wall does. That includes the row already highlighted and a wall already selected whose card was closed (before, both did nothing).
- Plan labels follow their members. Beam names run along the beam (left to right, or bottom to top on an upright beam, as the wall tags). Each column name sits off its square with a short leader and an arrow to it. The walls' name-and-length labels ("W11  39'") are off in this app (SketchCanvas.ShowWallLabels = false), because a click on a wall opens its card; the Building Model editor keeps them, now along the wall too. The sketch reports the label boxes it drew (SketchCanvas.MemberLabelBoxes), so the overlays keep off them without guessing. Landing leaders go out past those labels, and round the building's corner when their label is past the next side, never across the building (PlanLabels.Leader; test in PlanLabelsTests).
- Beam ends on a level plan, one at a time: each end with uplift shows its reaction "(2.7k)" along the beam just inside that end, and a small label of its own connection ("MSTI26") with an arrow at that end. The connection label goes outside the building when the beam is on its edge. Two beams on one column are two connections: their arrows land on their own beams, just inside each end (PlanLabels.BeamEnd, tests in PlanLabelsTests). The SHOW Reactions button now also shows on a level with beams and turns these on and off; the "↓ … on column C2" labels for hold-downs from above are unchanged, without the beam-end text. A beam end with no uplift (nothing lands on the beam, e.g. BM4 on junk) gets no label: gravity reactions are not calculated yet.
- Every beam end on a level is labelled: an end with no uplift shows "(0.0k)" and "None", in the same brown as the other connection labels. Where two beams meet at a column, a reaction whose spot is taken moves to the beam's other side, then further in along the beam (PlanLabels.BeamEnd nameSide).
- Hold-down markers: one marker for every plan, 25% larger (10 px, was 8; the foundation's anchors were 9). A dark square is a sized hold-down. An open red square, crossed, means none is strong enough. An open brown square means none is needed ("None" at a beam end with no uplift). On a level plan, every wall end with a hold-down (SHOW Design) and every beam end (SHOW Reactions) gets its own marker. The marker sits on its own member, just in from the end (PlanLabels.IconAt: its edge 2 px in), so two walls at a corner or two beams at a column each show their own. Each marker has its own small label ("HDUE5-SDS3", "MSTI26", "None") with an arrow to it, beside the member and outside the building first when the member is on its edge. The markers are drawn and reserved before the tags and labels, so those keep off them. The wall tags now show only the wall mark, since each hold-down is labelled at its end. Beam-end reactions start past the marker. Test: PlanLabelsTests.IconAt. Left as is: a landing triangle from the floor above can cover a marker at the same column (C1 on junk).
- SHOW Hold-downs is its own overlay, on by default. It covers the hold-down markers with their labels (wall ends and beam ends), what lands on the floor from the walls above, and on the foundation (G) the anchors. SHOW Design is now the wall tags and the schedule only. SHOW Reactions is now the reactions only: on a level, each beam end's "(2.7k)"; on G, what the foundation takes at each point. The badge at the end of the strip names "Hold-downs" when it is on.
- Hold-down catalog: a product picture beside the table, for the picked row's product line (the first row shown when none is picked). The pictures are local files in ShearWallVisualizer/HoldownPictures, copied beside the program by the build (any PNG added there is included). The app never loads a picture from the web. One picture per line, named by the model's leading letters (HDUE5-SDS3 → HDUE.png, LTTP2 → LTTP.png; Design.HoldownPictures, tests in HoldownPicturesTests). HoldownPictures/crop.ps1 crops a downloaded product photo locally: it trims the plain background, leaves a small margin and fits it in 280 px. Every line in the catalog has one (17): HDUE and LTTP from Simpson Strong-Tie's image server; HDU, DTT, HTT (and HTTH, the same page), LTTI, LSTHD, STHD, MSTC, MSTI, MSTA, LSTA, LSTI, ST, HRS and HTP cut from screenshots of their product pages (Edge headless, the page's main product photo), each trimmed by crop.ps1. A line without one shows "No picture of the … yet". Simpson owns the pictures; they are credited under each one.
- Wall construction window. Picking a wall on the plan, its tag or its table row opens it, and it follows the selection and the design. The card's Detail does the same for a wall, while beams and columns keep the Detail window. "Open when a wall is picked" turns the auto-open off for the session. The title uses the schedule mark ("SW2-9 (W9) · Level 2"), with the shear and hold-down status. Four tabs:
  - **Elevation**, to scale, low end on the left. Sill and double top plate laid flat, studs at 16" o.c., hold-down end posts, and blocking at the sheet joint. Face 1, face 2 or framing only, with nails on or off: edge nails red, field nails grey. The hold-downs sit on the end posts with an anchor rod into the foundation (or a strap for the strap families), labelled with model, fasteners, T and what each end anchors to (C1, beam BM2, the foundation, as traced). The hold-downs of the story above stand on the top plate as orange arrows with uplift, model and the upper wall's mark. Dimensions cover the sheet widths, length, height, 16" o.c. and the hold-down arm d. Callouts name the plates, studs (with their cut length), post, blocking, sill, sheathing and nailing.
  - **3D**: every framing piece, the sheets of each face (either face off, or the right half cut away to show the framing), the hold-downs and anchor rods, the straps from above and the foundation. Drag to turn, wheel to zoom, double-click to fit.
  - **Pieces**: what it takes to build. Framing (piece, size, quantity, cut length, notes). Sheathing per face (whole sheets and cut pieces by size, and sheets to buy). Nailing per face (nail and its size, edge and field spacing, approximate counts). Hold-downs per end (model, fasteners, allowable, T, what it anchors to). Connections at the top from the story above (position, from, model, uplift, the post under it). The design check, and the assumptions to confirm.
  - **Calculation**: the existing detail calculation table.
- The layout is pure and tested (Design.WallFraming, WallFramingTests, against hand counts). 2x6 on the first story and 2x4 above, plates as wide as the studs. End posts take as many 2x plies as fill the hold-down's minimum post, at least two. A two-ply post goes under each hold-down from above that lands along the wall rather than over an end post, and the stud there is dropped. 4×8 sheets stand up from the low end, so the joints land on studs, with blocking at the horizontal joint of a wall over 8 ft. Edge nails go 3/8" in, at the edge spacing, each corner once. Field nails are 12" o.c. on the studs inside a sheet. Sheets to buy counts whole sheets plus cut pieces stacked first-fit by height. Lengths are shown as feet-inches to 1/8" (FtIn).
- Beams and columns get the same construction window. Picking a beam or a column on the plan opens it, or the card's Detail does; it follows the selection. "Open when a wall, beam or column is picked" turns the auto-open off.
  - **Beam**: the elevation shows the span between its supports, each hold-down of the story above pulling up at its distance a, and each end's uplift reaction with its connection, fasteners and what it bears on. The 3D view has the beam, its supports, the straps and the pull from above. Pieces lists the beam, the end connections (uplift, connection, fasteners, allowable, bears on), the hold-downs on it from above (a, from, uplift, its share to each end) and the assumptions. Calculation shows R start = T (L − a) / L and R end = T a / L. On lbltest, BM2: 5,174 lb at a = 10.31 ft of 19.44 ft gives 2,429 / 2,745 lb.
  - **Column**: the elevation runs from its base to its top level, with what lands on its top and its base connection to the foundation. It also has 3D, Pieces and Calculation tabs.
  - Members are not sized; they are drawn 3 1/2 × 11 1/4 (beams) and 3 1/2 × 3 1/2 (columns) and labelled "not sized".
  - A catalog's bare fastener text ("28 0.162") now reads "28 0.162" nails" in these windows.
- Sill anchors for first-story shear walls (Design.SillAnchorage, tests in SillAnchorageTests, by hand). The Pieces tab has a Sill anchors section; the elevation and 3D show the bolts.
  - Bolt diameter is 1/2", or 5/8" when 1/2" bolts would need closer than 16" o.c.
  - Spacing is the less of 6 ft (IRC R403.1.6, IBC 2021 2308.3.1) and Z'/v (SDPWS-2021 4.3.6.4.1). v is the ASD shear 0.6 V / L. Z is from NDS-2018 Table 12E, Z∥ for a 1-1/2" sill on concrete: 1/2" 650 lb DF-L, 590 lb SPF; 5/8" 930 / 850 lb. Z' = 1.6 Z for wind (CD, Table 2.3.2), with CM, Ct, Cg and CΔ taken as 1.
  - End bolts sit not more than 12" (or half the spacing, so the bolt group carries v L) and not less than 7 diameters from each end. There are at least two per plate. The exceptions apply: one bolt for a plate section of 24" or less, none for 12" or less. Embedment is 7".
  - Washers (SDPWS 4.3.6.4.3): a 0.229" × 3" × 3" plate washer, reaching to within 1/2" of the sheathed edge where the nominal capacity is over 400 plf. Standard cut washers are allowed only under the exception: shear only, hold-downs sized without dead load, h:b ≤ 2 and nominal ≤ 1,370 plf.
  - The quoted code text is in an expander under the section.
  - Not checked: the seismic 4 ft rule (IRC R403.1.6.1, SDC D0–D2 over two stories), the hold-down anchor bolts, and the foundation.
  - On lbltest, SW2-2 (45'-6", v = 247 plf) gets 12 bolts of 1/2" at 4'-2" max (1,040 / 247 × 12 = 50.5"), laid out at 3'-11 1/2" and 12" from the ends: 12,480 lb ≥ 11,217 lb.
  - Sources: the IRC text is from Washington's adopted copy (WAC 51-51-0403); the IBC text is from Massachusetts 780 CMR 2308.6. The ICC site could not be read directly. SDPWS-2021 and NDS-2018 were checked against AWC's own PDFs.
- Sill anchor dimensions on the wall elevation: a chain under the hold-down labels, end distance at each end and the bolt spacing once with "TYP." when every space is the same (otherwise each space). An end too short for its text has it just outside the wall. The layout is now one a builder can tape: the spacing goes up to the next 1/2" (still within the maximum) and the two ends take what is left, never under 7 diameters (SillAnchorage.GapIn/EndIn; test LaidOut_SpacingToHalfInch_EndsTakeTheRest). On lbltest, SW2-11 (39'-0", 365 plf): 15 bolts, 10" + 14 × 2'-8" + 10".
- Esc closes the construction window (wall, beam or column) and goes back to the calculator: pressed in the window, or on the plan right after a pick (the window opens without taking the focus). On the plan the first Esc closes the window only; the next is the plan's usual Esc (clears the selection). An open drop-down or a text box keeps its own Esc. Checked in the app both ways.
- Stud size per wall, with the code check (Design.StudRules, tests in StudRulesTests by hand; WallFramingTests.ChosenStudSize_SetsStudsAndPlates).
  - WallDesignInputs.StudSize: null = the story's default (2x6 on the first story, 2x4 above); else the engineer's 2x4, 2x6 or 2x8. Saved with the wall's design, undoable, logged. "Use for new walls" clears it, so new walls take their own story's studs. The framing (studs, plates, posts, blocking) follows it in the elevation, 3D and pieces.
  - The construction window has a Studs drop-down at the top right. It offers the default, then only sizes at least as big as the default and the code's smallest here (it only increases). A default under the code's minimum is shown in red.
  - The check uses IRC Table R602.3(5), whose cells are the same as IBC Table 2308.9.1 (2308.5.1 before the 2024 IBC). It covers bearing walls with laterally unsupported studs up to 10 ft. Spacing limits (roof only / one floor + roof / two floors + roof): 2x4 24" / 16" / not permitted; 2x6 24 / 24 / 16. 2x8 is not in the table and is taken as at least a 2x6.
  - Design is required, and no prescriptive size is given, for: studs over 10 ft, three or more floors above, studs over 24" o.c., or wind ≥ 140 mph in Exposure C or D (IRC R301.2.1.1 and IBC 2308.2.4; Exposure B is allowed to 140 under the IBC). The wind speed and exposure come from the wind model; manual loads skip that part.
  - SDPWS-2021 4.3.7.1 item 5: with nails at 2" o.c., or 10d at 3" o.c. or less, a 3x nailed face is needed at panel joints (or two 2x fastened per 4.3.6.1.1), with staggered nailing. This is noted under Pieces.
  - Pieces has a "Studs: code check" section, with the quoted text in a Sources expander. The window's subtitle says whether the studs meet the table.
  - Sources: the 2025 California Residential and Building Codes (2024 IRC and IBC) on gocodebook.com, and AWC SDPWS-2021 Ch. 4. The table cells were checked against the downloaded text. The 2018 and 2021 editions could not be opened; their cells are believed to be the same.
  - Not done: the NDS 3.9 stud design (bending and compression), and IRC R602.3.1 Exceptions 2 and 3 (tall 2x6, Table R602.3(6)), which are cited only.
  - On lbltest, every wall is 15 ft, so its studs are 14'-7 1/2": design required.
  - Checked in the app on lbltest: SW2-11 (Level 2, 2x6 default) offers 2x6 and 2x8; picking 2x8 redraws the studs, plates, posts and blocking as 2x8 (7 1/4" sill); Ctrl+Z puts it back to the default and Ctrl+Y brings it back. SW3-10 (Level 3) offers 2x4, 2x6 and 2x8 and reads "the roof and ceiling only". Every wall there is 15 ft, so it shows "design required".
- Ctrl+Z and Ctrl+Y (Ctrl+Shift+Z) in the construction window undo and redo the model, as on the plan (they did nothing there before).

### Wall framing moved out of the calculator (building model + shared libraries)
Wall framing isn't only a shear-wall matter, so it moved out of the calculator. The building model is the backbone for every app: wind, this calculator, the gravity load path to come, and the foundation.
- **The building model** (C:\Programming\BuildingModel, file format 5) stores each wall's studs per story, keyed by the level on top of the story. `Wall.Framing` holds `StudFraming { StudSize, StudSpacing }`. The project defaults are in ModelDefaults: `StudSizeFirstStory` 2x6, `StudSizeAbove` 2x4, `StudSpacing` 16".
  - `Building.StudSizeOf` and `StudSpacingOf` give the wall's own value, else the project's. `IsFirstStory` tells which default applies.
  - `BuildingEdits.SetStudFraming` sets a size and spacing, or clears them back to the default.
  - An older file reads the defaults.
  - Tests: FramingTests.
- **WoodFraming** (new local repo, C:\Programming\WoodFraming; no UI). It has WallFraming (the pieces, sheets and nails), FtIn, StudRules (the IRC/IBC stud table check) and SillAnchorage, with their tests.
  - WallFraming.Build now takes the stud size and spacing; the hard-coded 2x6/2x4 is gone.
  - A size it can't draw, or no spacing, is refused.
- **NDS_WoodLibrary** (C:\Programming\NDS_WoodLibrary, local branch dev/nds2018). It is now an SDK-style project, and the 2021 files still build. The 2018 NDS is in NDS2018\:
  - Species: DF-L, HF, SPF, SP, with their G values.
  - Table 12E: sill-to-concrete Z∥ for 1/2" and 5/8" bolts, for all four species.
  - Tests: NDS_WoodLibrary.Tests.
- **The calculator** keeps only the shear-wall part: ShearWallFraming has SDPWS 4.3.7.1 item 5 (a 3x face at panel joints) and item 6 (24" max).
  - WallDesignInputs.StudSize was removed. A stud size picked in the construction window is now an undoable edit of the model through the sketch, so every app sees it.
  - The picker's first item is the project default for the story.
  - The sill bolts use the NDS library's Z, with the calculator's species group mapped to DF-L or SPF, the same values as before.
- Not yet: a defaults page for the stud defaults in the Building Model editor (the editor has no defaults page yet); per-wall spacing in the UI. The model stores spacing and the framing uses it, but there's no picker yet.
- The builds and tests pass: SWC 219, WoodFraming 22, NDS 2, BuildingModel 145.
- Checked in the app on lbltest. SW2-11 offers "2x6 (project default, first story)", 2x6 and 2x8; picking 2x8 redraws the wall as 2x8 (logged "SW2-11 (W11): studs 2x8."). Ctrl+Z goes back to the default and Ctrl+Y returns 2x8. SW3-10 offers "2x4 (project default, upper story)" and up. Saving writes `"Framing": {"L2": {"StudSize": "2x8"}}` to the version 5 file, with the stud defaults.

### NDS stud design (walls outside the prescriptive table, e.g. over 10 ft)
- **NDS_WoodLibrary** (branch dev/nds2018) adds:
  - Table 4A for DF-L, HF and SPF in No. 2, No. 3 and Stud, with the size factors CF. Stud 8" and wider takes the No. 3 values, per the table's note.
  - Table 4B for Southern Pine (size-specific values).
  - Table 1B sections.
  - CD from Table 2.3.2, with the shortest load in a combination governing (2.3.2.2).
  - Cr = 1.15 (4.3.9), CP (Eq. 3.7-1, c = 0.8, le/d ≤ 50) and Eq. 3.9-3.
  - Checked against the AWC 2018 PDFs.
  - Tests are hand-checked, for example a DF-L No. 2 2x6 at le = 175.5" with CD 1.6 gives CP = 0.18833.
- **WoodFraming.StudDesign** checks one stud:
  - Axial load is the wall's top loads (D, L, Lr, S in plf) over the stud spacing.
  - Bending is from the C&C wind (exterior) or 5 psf (interior, IBC 1607.16), simply supported between the plates. The sheathing braces the weak axis.
  - Combinations come from the LoadManager (see below). A combination with a load the stud doesn't carry is skipped. CD comes from the shortest load in the combination.
  - Also checked: bearing on the plates (Fc⊥, Cb = 1), shear, and deflection (IBC Table 1604.3, ℓ/240 by default, wind at 0.42 × C&C per note f; E').
  - Hand-checked: D + L + 0.6W for D 300, L 400, W 30 psf on a 2x6 gives 0.6876.
- **The building model** holds the project's stud species and grade (DF-L No. 2) and the wall deflection limit (240). It holds no loads.
- **Construction window, Pieces**: when the stud table doesn't apply, a "Studs: NDS design" section appears.
  - It has D / L / Lr / S boxes. A change is saved to the LoadManager's "Loads" section through the sketch, so it's undoable and logged.
  - It shows the stud and its reference values, le/d and FcE, the lateral load and where it came from, a table of every combination (CD, P, fc, CP, Fc', fb, Fb', Eq. 3.9-3), the other checks, and the quoted sources.
  - The subtitle gives the NDS result.
- **Wind on the studs**: the wind model's own C&C calculator gives p = qh Kd (GCp ∓ GCpi), the larger of inward and outward, at least 16 psf.
  - GCp is read from Fig. 30.3-1 at the stud's effective area: span × max(spacing, span/3).
  - Zone 5 is used where the wall comes within a of a building corner, Zone 4 elsewhere.
  - An exterior wall with manual loads isn't designed; the section says the wind model is needed.
- Not checked against the printed text:
  - Ke = 1.0. Table G1 is an image in the PDF.
  - The effective wind area rule (ASCE 7 26.2) and the Fig. 30.3-1 zone width; the wind library uses the same a as Fig. 28.3-1.
  - Snow is taken as ASCE 7-22 gives it, with 0.7 S, following the 2024 IBC.
- Checked in the app on lbltest:
  - SW2-11 (2x8, interior, D 300 / L 400): D + L + Li governs at 0.16 (now named D + L: the 5 psf is a live load). By hand: Fc' = 1417.5 × 0.483 = 685 psi, Fb' = 1242 psi, fb = 163 psi.
  - SW2-1 (2x6, exterior): 37.2 psf from Zone 5 (34.2 × 0.85 × (1.10 + 0.18)). With D 300, L 400, S 250 all ten combinations appear and D + L + 0.6W + 0.7S/2 governs at 0.89 (P = 1,050 lb).
  - Deflection 0.645" ≤ 0.731". Ctrl+Z restores the previous loads.

### LoadManager: loads out of the building model
Loads aren't the building model's. Like the wind calculator's "Wind" section, they come from their own system.
- **LoadManager** (C:\Programming\LoadManager, a new local repo with no remote) references only BuildingModel.Core.
  - LoadSection: the "Loads" section of the project file. For now it holds the wall-top line loads (D, L, Lr, S in plf) by model wall ID and the story's top level ID. SetWallTop is one edit, so it's undoable through the sketch. All zero removes the entry.
  - LoadCombinations.IbcAlternativeAsd(): the 2024 IBC 1605.2 alternative ASD set, Eq. 16-1 to 16-6, with the quoted source. Each equation is expanded with its variable loads zeroed in turn (the CA amendment's wording), without repeats.
  - No combination is dropped (the user: "do not drop IBC and ASCE7 load combinations. All must be analyzed"). One that several equations give is listed once, with every equation it comes from.
  - LoadCombinations.Asce7Asd(): ASCE 7-22 2.4, Eq. 1a to 10, with (W or WT), (Lr or 0.7S or R) and Ev/Eh (0.6D − 0.7Ev + 0.7Eh) written out. Asce7Strength(): 2.3, Eq. 1a to 7. Asd(): both ASD sets, the stud design's default.
  - The ASCE 7-22 equations are as printed in ICC, "Demystifying Loads for Building Officials – 2024 IBC / ASCE 7-22", slides 62–64 (the standard itself is paywalled; not checked against it). That PDF's text was read with a .NET decoder (FlateDecode + ToUnicode CMaps).
  - The construction window lists every ASD combination with its equations ("ASCE 4a, 6a; IBC 16-1"). SW2-1 shows the same governing D + L + 0.6W + 0.7S/2 at 0.89; ASCE 6a D + 0.75L + 0.75(0.6W) + 0.75(0.7S) gives P = 975 lb, as by hand.
  - Floor and roof area loads, and the gravity load path that turns them into wall-top loads: see the next section.
- **BuildingModel**: WallTopLoad, Wall.TopLoads, TopLoadOf and SetTopLoad are removed. The stud lumber and deflection defaults stay.
- **WoodFraming.StudDesign** takes LineLoad and the LoadManager's combinations (the IBC set by default, or the caller's). Durations follow NDS Table 2.3.2 (StudDesign.Duration); rain isn't in the table and throws. The interior 5 psf is a live load (L), so it goes with L.
- **SWC** reads and writes the wall-top loads through LoadSection.
- Tests: LoadManager 9, WoodFraming 31, BuildingModel 146, SWC 219.
- Checked in the app on a copy of lbltest (lbltwo):
  - SW2-1 with D 300 / L 400 / S 250 gives the same ten combinations, with D + L + 0.6W + 0.7S/2 governing at 0.89.
  - Ctrl+Z and Ctrl+Y step through the edits.
  - The saved file holds `"Loads": {"WallTops": {"W1": {"L2": {...}}}}` beside "Wind", and nothing on the wall itself.

### Floor and roof loads and the gravity load path (first pass)
- **BuildingModel (format 6)**: a diaphragm's span direction, which way its joists or rafters span (Auto: the shorter way of its pieces). SetDiaphragmSpan. It's construction, so it's in the model. No editor UI for it yet; Auto applies.
- **LoadManager**:
  - LoadSection.Levels holds area loads per level (D, L, Lr, S in psf, on the plan). LoadSection.WallDead is the walls' own weight (psf of wall). SetLevel, SetWallDead and SetAreaLoads (one edit). LineLoad and AreaLoad share GravityLoad.
  - GravityLoadPath.Run works top-down:
    - The framing spans one way: the diaphragm's span, or for a sloped roof across the ridge, eaves included.
    - Each bearing wall or beam line takes half the distance to the next one on each side, or all of it to the edge where there is none.
    - It's sampled every 0.25 ft along the wall, and the most along the wall is kept.
    - A wall standing on a wall below (the same wall going on up, or a wall on its line) brings its load plus its weight (WallDead × story height).
  - Notes: a side with no support past an eave ("a cantilever, or a support not drawn"), and a wall standing on the floor framing (not traced).
  - Not traced yet: beam reactions, columns, point loads, and walls on the floor framing.
  - GravityLoadPath.Effective: the wall-top load entered by hand wins, else the load path's.
  - LoadManager.Wpf (new): AreaLoadsWindow, a code-only WPF window with a row per level (its framing described), D/L/Lr/S boxes and the walls' weight.
- **SWC**:
  - Loads > Floor and roof loads... opens the window. OK applies it as one sketch edit (undoable).
  - The construction window's NDS section shows the load path's wall-top load and how it was found (tributary, walls above, notes). The boxes are now the hand override; clearing them all goes back to the load path.
- Tests: LoadManager 19 (hand calculations: one story 6/10/4 ft tributaries, a partial interior wall 6–10 ft, a span set by hand, two stories D 312 = 72 + 150 + 90, a continuous wall, a gable with 2 ft eaves = 12 ft, missing support, a wall on framing, the hand entry winning, round trip), BuildingModel 148, WoodFraming 31, SWC 219.
- Checked on lbltwo (the screen was locked, so through UI Automation and the saved file):
  - The window took D 15 / Lr 20 / S 30 (Level 3, the roof) and D 12 / L 40 (Level 2), walls 10 psf. The file saved them (version 6).
  - The load path on that building: the gable rafters (ridge W-E) put 19.5 ft on W6 and W8, giving D 292.5, Lr 390, S 585. W2 and W4 get D 234 + 292.5 + 150 = 676.5 and L 780, as by hand.
  - It notes W3 standing on the Level 2 framing.
  - The construction window's display is still to be seen on screen.

### Span picker in the editor; beams, columns and concentrated loads in the gravity load path
- **BuildingModel editor**:
  - A diaphragm's properties have a Span picker: "Auto (S-N, the shorter way)", W-E or S-N. Picking one is one undoable edit.
  - The picker is off for the sloped roof, whose rafters run across the ridge.
  - The plan label shows the span ("D1 · spans S-N").
  - Checked in the app on a scratch copy (spantest): set W-E, the label followed, undo put it back.
- **LoadManager** (GravityLoadPath):
  - **Beams:** a beam takes its share of the floor by tributary width, like a wall (the most along it, over the whole span).
    - A wall standing on a beam is a part-length load on it: the wall's load plus its own weight.
    - Beam ends and columns landing on a beam are concentrated loads on it.
    - Reactions are simple span by statics, each load kind alike.
    - Each end lands on what is under it: a wall, then a column, then a beam (a girder, only away from its own ends).
    - Beams framing into a girder are done before the girder; a loop is noted.
  - **Columns:** a column carries the beam ends landing on it down to its base: the foundation, or a wall, beam or column under it.
  - **Concentrated loads on walls:** a beam end or column base on a wall is a concentrated load on that wall's top, at its place along the wall.
    - It is carried on down the walls under it ("BM1 via W6"), each needing a post. It is not in the stud check.
  - **Tolerance:** a hand-drawn beam within 0.25 ft of square counts as square, the same tolerance the model uses for column bases. An edge within 0.25 ft of a line gets no note.
  - New types: PointLoad, ConcentratedLoad, PartialLoad, BeamLoad, ColumnLoad; Result.Beams and Result.Columns.
  - A wall standing on a beam is no longer noted as "on the framing".
- **SWC**:
  - The construction window lists a wall's concentrated loads (from, where along the wall, D/L/Lr/S lb) under the load-path line.
  - A beam's and a column's Calculation tab add the gravity lines: the floor or roof tributary, the walls and loads on it, each end's reaction and what it bears on, and for a column, what it carries and what it stands on.
- **Tests**:
  - LoadManager 24. The new hand calculations:
    - A beam on posts: 10 ft tributary, 1500 / 3000 lb ends.
    - A beam on walls: 1500 lb at 10 ft along the wall.
    - Two stories with a wall on a beam: D 2500, L 6000, Lr 1000 each end; the S wall D 250, L 200, Lr 200.
    - A beam into a girder: 750 into the girder, then 375 to each wall.
    - A roof beam's end carried down the wall under it.
  - BuildingModel 148, WoodFraming 31, SWC 219.
- **Checked on lbltwo** (scratch build):
  - BM1 (13.5 ft) takes 19.53 ft of Level 2 (D 234, L 781 plf) plus W6 on it (D 292.5 + 150 own weight = 443, Lr 390, S 585). Each end: D 4,569, L 5,273, Lr 2,633, S 3,949 lb, as by hand. It bears on W2 and C2.
  - SW2-2 (W2) lists BM1 at 45'-6" as a concentrated load. C1 carries BM3 and BM2 (W3 stands on BM2) to the foundation.
- **Not done yet:**
  - Sizing beams, columns and posts.
  - Continuous beams, cantilevers and beams that are not square to the plan (only what stands or lands on them is carried).
  - Spreading a concentrated load into a wall below (it stays a post load).
  - Area-load zones.

### Concentrated loads checked; post loads in the stud design
- **Bug fixed (LoadManager):** a concentrated load in an upper wall went to every lower wall within 0.25 ft of it.
  - At the joint between two lower walls on one line it was counted twice. A test caught it on the old code: 2 loads instead of 1.
  - A load with no lower wall on its line was dropped without a note.
  - Now each concentrated load in a wall standing on a level goes to exactly one support: the nearest wall or beam under it on its line, else whatever is under the point (a cross wall, a column, a beam). If nothing is there, it is noted.
- **WoodFraming (StudDesign):** the input takes the wall's concentrated loads (Points). For each one, a PostLoad gives the post's load.
  - The post carries the concentrated load plus the wall's line load over one stud spacing.
  - Axial only, in every ASD combination of the loads on it (dead always counts), CD by the shortest load (NDS 2.3.2.2).
  - It reports the largest P, and the largest P/CD (the one that governs a wood post of any size). No sizing.
- **SWC:** the construction window's post table lists each post: where it is, the concentrated load, the post total, the most P, and the governing P/CD.
  - The hand-entered line load replaces only the line load; the concentrated loads stay the load path's (said in the note).
- **Tests:**
  - LoadManager 25: the joint case, a 1500 lb roof beam end over two lower walls, goes to one.
  - WoodFraming 32: post hand calculation.
    - D 1000, L 2000, Lr 500 on a 300/400 plf wall at 16" gives D 1400, L 2533.33, Lr 500.
    - Most P: D + L + Lr = 4433.33 at CD 1.25.
    - Governing a wood post: D + L = 3933.33 at CD 1.0.
    - No wind or snow combinations.
  - SWC 219.
- **Checked on lbltwo**, SW2-2 (W2) under BM1, matching the hand calculation:
  - Post total: D 4,569 + 677 × 16/12 = 5,471; L 6,313; Lr 3,153; S 4,729.
  - Most P: D + L + 0.7S = 15,095.
  - Governing: 15,095 / 1.15 = 13,126.

### Foundation loads; hand-entered wall loads carried down
- **LoadManager:**
  - GravityLoadPath's Result now has Foundation. It lists each wall on the ground: its line load at the base (its top load plus its bottom story's own weight) and the posts in it. It also lists each column on the foundation with its load.
  - LoadCombinations.Largest(load) gives the largest ASD total of a gravity load and its combination. It uses the combinations whose loads are all on it; wind and earthquake add nothing vertical and are left out.
- **Hand entries carry down:** a wall-top load entered by hand now replaces the computed one inside the load path itself. It goes on down to the walls, beams and foundation under it.
  - Before, only that wall's stud design used it.
  - WallTopLoad.Computed keeps what the path found; WallTopLoad.Entered marks a hand entry.
  - The construction window's "From the floor and roof loads" line shows the computed value.
- **LoadManager.Wpf:** FoundationLoadsWindow, a read-only window opened from SWC Loads > Foundation loads...
  - Walls: length, line load at the base by kind, largest ASD, posts.
  - Columns: place, load, largest ASD, what lands on them.
  - Notes: hand entries and the load path's remarks.
- **Tests:** LoadManager 26.
  - Foundation: S1 D 350, L 200, Lr 200; largest D + L + Lr = 750 plf; C1 9,500 lb.
  - A hand entry of D 1000 upstairs gives 1,150 on the wall below and 1,250 at the foundation.
  - WoodFraming 32, SWC 219.
- **Checked on lbltwo:**
  - W2 at the base: D 676.5 + 150 = 827; D + L + 0.7S = 2,016 plf.
  - W1 with its hand entry: D 300 + 150 = 450; D + L + 0.7S = 1,025 plf, noted.
  - C1 carries BM3 and BM2: 13,846 lb.

### Load zones
- **LoadManager:** LoadSection.Zones holds LoadZone entries: level, name, a rectangle on the plan (ft), and area loads.
  - A zone's loads take the place of the level's inside it, for example a deck, a corridor or a storage room. Where zones overlap, the larger of each kind applies.
  - SetZones sets them all as one edit. It puts the corners in order, takes negative loads as 0, and leaves out zones with no area or no such level.
  - Saved in the "Loads" section of the project file, not the model.
- **Load path:** at each sample along a wall or beam, the line load is the area load summed across that point's own tributary strip. A zone counts over the part of the strip it covers.
  - For each kind, the most along the line is kept.
  - With no zones this is the old result: area × tributary.
  - WallTopLoad.Zones and BeamLoad.Zones name the zones met; the construction window's load-path line says "load zone Storage".
  - A zone only counts where a sample point is inside it by more than 0.01 ft; a line that just grazes its edge does not take it.
  - Two bearing lines within 0.25 ft of each other count as the same line. A beam drawn 3/4 in off a wall's line was splitting the wall's tributary.
- **Floor and roof loads window:** a Load zones table, one row per zone: level, name, west x, south y, width, depth, D, L, Lr, S, Remove.
  - "Add zone" starts a zone with that level's loads as typed.
  - Bad numbers are marked; the width and depth must be above 0.
  - OK applies the levels, the walls' weight and the zones as one undoable edit.
- **Tests:** LoadManager 30.
  - A deck over part of the length: L 360 on the edge wall, 600 on the interior wall.
  - A corridor over half the strip: 3 × 100 + 3 × 40 = 420.
  - A line grazing a zone's edge takes the level's 400, not 600. This fails on the old inclusive edge.
  - Round trip and clear.
  - WoodFraming 32, SWC 219.
- **Checked on lbltwo** (scratch copy; the original is kept as lbltwo_before_zones.bldg.json):
  - Added "Storage" on Level 2 (x 30–46, y 0.5–20, D 12, L 125) through the window, pressed OK, saved.
  - The file holds the zone.
  - W4 takes L 125 × 19.5 = 2,437.5 plf, with tributary 19.5 all along. W2 is unchanged.
  - The construction window reads "load zone Storage".
- **Not done yet:** drawing zones on the plan (they are typed in); zones on a sloped roof's slope.

### Wind uplift on the roof (from the wind calculator)
- **The pressures are the wind calculator's own.** Nothing is calculated a second time: the same results the shear walls take their wind from (Wind setup, linked).
  - **Whole-building (MWFRS, Ch. 27):** the roof zones of Fig. 27.3-1 as the calculator lays them out (plan polygons), both roof coefficients (the larger suction and the other one), with the wind from each of the four sides: 8 cases.
    - Wind from the east and from the north are the calculator's west and south results mirrored. The south one is the calculator's along-width run turned onto the plan.
    - Internal pressure taken to lift the roof (external − |internal|).
    - The eaves take the top pressure of the zone they come out of, without internal pressure. The windward eave also takes the soffit, Cp 0.8 (27.3.3).
  - **Components and cladding (C&C, Ch. 30 Part 1), for the rafter or truss ties:** the calculator's roof zones and curves, read at each rafter's effective wind area, span × max(spacing, span/3) (26.2). Internal pressure and the 16 psf minimum (30.2.2) apply. The eaves use the overhang coefficients (ASCE 7-22 with none: roof + wall below, as the calculator does).
    - The span is the load path's: from the wall or beam to the next one across the framing (WallTopLoad/BeamLoad.FramingSpan).
    - At 10 sq ft the C&C pressures equal the calculator's own tables (tested, 7-16 and 7-22). The wind setup's default 10 sq ft would have given about 1,500 lb per truss on lbltwo, against about 600 at the truss's 507 sq ft.
- **LoadManager:**
  - GravityLoad.Wind, W (+ down, − up). It is never entered or saved.
  - Net = 0.6D + 0.6W (ASCE 7-22 2.4 Eq. 7a; the IBC 1605.2 alternative counts 2/3 D, less severe).
  - GravityLoadPath.Uplift(b, s, case) is the same load path with the dead loads only (levels, zones, walls' weight, and hand entries' D) and the case's pressure taken across each roof tributary strip, eaves included.
    - W goes down like any load: to walls below, beam reactions, posts, columns, and the foundation.
    - Along a line, the sample with the most net uplift is kept, its D and W together. Taking the most D with the most W would not be conservative.
  - RoofWind.Mwfrs / RoofWind.Cladding build the cases from the calculators.
  - WindUplift.Run gives the governing case for each wall top, beam end, column, foundation wall and post, and the C&C tie at each roof wall and beam. It makes one pass per distinct rafter area.
  - LoadManager now references ASCE7Wind.Core (no UI).
- **BuildingModel:** ModelDefaults.RafterSpacing = 24 in. (the roof framing; not editable in a window yet).
- **SWC:** Loads > Wind uplift... opens the load manager's read-only window.
  - It shows ties (span, area, D, W, net, per rafter), wall tops, beam ends, columns, and the foundation (walls and their posts), each with its governing case.
  - With manual wind loads it says there are no roof pressures.
- **Tests:** LoadManager 40 (10 new). BuildingModel 81. SWC 219.
  - Uniform 30 psf up: S wall D 90, W −180, net −54; interior −90; N −36. Live load dropped.
  - Varying wind plus a heavier zone: the place with the most net uplift, not the most D with the most W.
  - Two stories: 312 D vs −300 W gives +7.2 (held); the foundation gives +67.2.
  - Beam on two posts: −1,800 lb each to the foundation.
  - Hand entry keeps its D and takes the W. The eave is in the strip.
  - Envelope: governing case and the tie, 12 ft span → 48 sq ft, −216 lb a rafter.
  - The calculator's zones on the plan, by hand: qh Kd 24.43, −23.09 psf at the windward edge in all four directions, −35.30 on the windward eave.
- **Checked on lbltwo**, through the window and a script with the app's DLLs:
  - Gable 26.6°, 7-22, 115 mph.
  - The W8/W6 roof share is 19.5 ft × −29.3 psf (wind from the west, 0 to h/2) = −571 plf; against D 293 that is 167 plf up.
  - Trusses 39 ft, 507 sq ft: 617 lb up a truss at 24 in.
  - Below the roof, the dead load holds everything down: W2/W4 at Level 2 63 plf, posts C1 1,116 lb, C2 427 lb.
- **Not done yet:**
  - The wind on a lower roof (only the top roof's).
  - A window to set the rafter spacing.
  - Uplift in the construction window.
  - Hold-down sizing for the uplift (only the forces are found).

### The Load Manager app; the foundation's loads combined (plan, envelope, 3D)
- **The gravity loads now live in their own app.** The Shear Wall Calculator only reads them.
  - Floor and roof loads, load zones, the walls' own weight and a wall's top load by hand are edited in the Load Manager (LoadManager.App, LoadManagerApp.exe).
  - It opens the same .bldg.json. It never changes the building; Save reads the file again and replaces only its "Loads" section.
  - It reads the building again when another app saves the file. Unsaved load edits are kept.
- **Load Manager, Loads view:** the model's plan (read only; its drawing tools get no clicks or keys) with a level bar.
  - Zones are drawn on the plan (R, drag), then edited or deleted in the side panel. Each wall's top load is labelled.
  - Click a wall: what it carries, where it comes from, how it varies along it, its posts, and its top load by hand (set or clear).
  - Undo and redo cover the loads (snapshots of the Loads section).
  - Loads menu: Floor and roof loads, Wind uplift (from the file's wind setup), Foundation loads (service), Foundation loads (combined).
- **Foundation, combined (LoadManager.FoundationAnalysis):**
  - Walls on the ground: the line load as it varies along each wall (the load path's profiles), cut wherever any wind case changes.
  - Points: posts in the walls, columns (on a wall's footing when they stand in it, else on their own), and the shear walls' end forces.
  - Wind: the roof's 8 whole-building cases carried down the load path, plus the shear walls' couples per case. Where both meet, their extremes are added whatever their cases (the two apps name cases differently). That is on the safe side.
  - Every combination of the method (ASD: ASCE 7-22 2.4 and IBC 1605.2; or strength: ASCE 7-22 2.3) with only D, L, Lr, S and W, and only those whose loads are present.
    - Each variable load is set to zero where that is worse: "most" takes the wind at its most pressing down, "least" at its most lifting.
    - E, Ev, Eh, R and WT combinations are left out, and the window says so.
  - The envelope: the most down and the most up, with the combination giving each, for every stretch of footing and every point.
- **Foundation view:**
  - On the plan: beside each footing the most combined line load down (blue, to scale) and the most up where it lifts (vermilion). Circles are posts and columns; squares are shear wall ends.
  - Under the plan: the envelope load diagram of the selected wall (most and least along it, the points as arrows with their values).
  - The side panel: ASD or strength, the overall envelope, the selected wall or point with its service loads and every combination (collapsed), and "All the numbers…" (each combination's load factors, every wall and point).
  - **Foundation 3D:** the building see-through and grey. Load-diagram plates stand on the footings (blue down, vermilion up), with arrows at the points. The numbers are on the plan.
- **The shear walls on the foundation:** on every save the Shear Wall Calculator writes the "Lateral" section (LoadManager.LateralSection).
  - It holds each first-story wall end's overturning couple per wind case, at W level (the ASD couple ÷ 0.6), with no dead load.
  - Column bases carrying hold-downs are included as ASD ÷ 0.6.
  - The Load Manager adds its own dead loads and combines. It warns when a named wall is gone or has moved, or when the model has been saved since.
- **SWC:** Loads > Edit loads in the Load Manager... opens it like Edit building (save first, wait under a notice, reload on each save).
  - The Floor and roof loads window and the construction window's hand entry are gone. The construction window shows a hand entry read only, with an "Edit loads…" button.
- **BuildingModel:** Building3DView.Ghost (see-through, grey) and Extra (the host's own 3D models).
- **Tests:** LoadManager 46 (3 new). SWC 219.
  - Gravity ASD: 190 / 120 plf → D + Lr 310, least 0.6D 114. Strength: 1.2D + 1.6Lr 420, least 0.9D 171.
  - Roof lifted 30 psf: W −180 plf; ASD 0.6D + 0.6W = 6 plf (down); strength 0.9D + 1.0W = −9 plf (lifts).
  - A shear wall end ±1,000 lb: ASD ±600, strength ±1,000. One with nothing drawn under it stands alone, with a note. The Lateral format round-trips.
- **Checked** on scratch copies of lbltwo, rendered in the app's own window in-process (the display was off):
  - W4 footing: most 3,674 plf (D + L + 0.7S), least 153 plf (0.6D + 0.6W).
  - C1, strength: 1.2 × 5,751 + 1.6 × 5,305 + 0.5 × 2,657 = 16,719 lb; least 0.9 × 5,751 − 3,892 = 1,284 lb.
  - The SWC save wrote 13 shear wall points (64 cases). The Load Manager shows them; the W1/W2 corner takes ±9,045 lb ASD.
  - Edit loads from SWC's menu opened the Load Manager; the notice showed and cleared when it closed.
  - A Load Manager save changed only the loads: wind, lateral and walls unchanged, revision 26 → 27.
- **Not done yet / to confirm:**
  - The shear wall app's own wall dead-load input (plf per wall) is a second source beside the Load Manager's. It should read the Load Manager's.
  - The shear-wall points carry no dead load of their own: the footing's line dead load resists them, but the points' envelope shows the bare uplift.
  - Earthquake loads.
  - Footing sizing.
  - Labels in 3D.
  - A load manager for beams and columns (the framing design app).

### No dead load and no gravity work in the Shear Wall Calculator (2026-11-04)

The Shear Wall Calculator only finds hold-downs. It no longer counts dead load against them and does no gravity or stud design.
- **Hold-downs:** T = 0.6 M / d at ASD (1.0 M / d at LRFD, for reference), plus the walls above. There is no dead-load reduction.
  - The wall table's Dead (plf) column is gone, along with the value saved with each wall. An old file's value is ignored when read.
  - The "Dead load" calculation line is gone; the "Own uplift" line says no dead load is counted.
  - When no hold-down is strong enough, the hint still mentions dead load, saying this app does not count it.
  - The sill anchors take the SDPWS 4.3.6.4.3 washer exception where it otherwise applies, since the hold-downs are sized without dead load.
- **Foundation forces (G view, and the Lateral section for the Load Manager):** each wall end's overturning couple per case alone, + up / − down.
  - `EndForceByCase` is now [low, high]; it no longer carries the 0.6D and 1.0D versions.
  - `FoundationLoad.ByCase` replaces `With06D` / `With10D`. The plan shows uplift and overturning compression from the wind alone.
- **Removed from the construction window:**
  - The prescriptive stud table check (IRC R602.3(5)), which is out of scope for these projects.
  - The NDS stud design, including the C&C wind on the studs and the gravity loads on the wall's top.
  - The posts-under-concentrated-loads table and the "Edit loads…" button.
  - The stud size picker stays, for the drawing; it offers the project default and larger sizes, with no code filter.
  - A new "Shear wall framing" section keeps the SDPWS 4.3.7.1 shear wall rules: a 3x at the panel joints for tight nailing, and studs at 24" o.c. or less.
- **Removed elsewhere:**
  - The gravity lines in the beam and column views.
  - Loads > Foundation loads and Loads > Wind uplift. The Load Manager has both.
  - The Loads menu now has only "Open in the Load Manager...".
- **WoodFraming:** `StudRules` (the prescriptive table) and its tests are deleted. `StudDesign` (NDS 3.9) stays in the library for a future framing design app, which is where the wind-on-the-face and gravity stud checks belong.
- **Tests:** SWC 219, all passing, with the dead-load hand calculations redone without D:
  - 0.6 × 10 × 1000 × 10 / 9.75 + 500 = 6,653.8 lb.
  - LRFD: 100,000 / 9.75 + 800 = 11,056.4 lb.
  - WoodFraming 27, all passing; the StudRules tests went with the class.

### Wind from the code and commentary; one wind source for every app (2026-11-04)

**ASCE 7-16 and 7-22 Ch. 27 and 28, checked against the printed code and commentary** (read from C:\AllenEngineering\Codes):
- **The 16/8 psf minimum is a separate load case**, one per wind, with no torsion and no internal pressure (C27.1.5; C28.3.4 in 7-16, C28.3.6 in 7-22). Cases 1–4 are built on the calculated force. This replaces the earlier "raises the force" rule.
- **Flat roofs (Ch. 28):** run as though they had a ridge at mid-building, once each way (Case A and Case B in each direction).
- **Hip roofs (Ch. 28):** now covered, per Fig. C28.3-3. The Case 1 roof coefficients are used for both load cases.
- **Zone width a:** uses the eave height for θ ≤ 10°, the a ≤ 0.8h cap, and the large-building exception (least dimension > 300 ft).
- **7-22 Ch. 28 printed values:** the torsional T table as printed, and Kz 0.70 for Exposure B with z < 30 ft (Table 26.10-1 note a).
- **7-22 Fig. 27.3-8 note 2:** in Cases 3 and 4 each roof area takes 100 % (Case 4: 75 %) of the larger of its two winds' roof pressures. 7-16 keeps 75 % / 56.3 % of each.
- **Fig. 27.3-1:** a value that isn't printed is 0. Note b's area reduction is applied and cited, with the unreduced value shown beside it in the wind calculator.
- **Ch. 28 parapets:** not covered (cited); the Ch. 27 parapet force is carried instead.

**One wind source:** the Wind Calculator's own case list is the only wind for every app.
- `WindCases.For` returns that list for the setup's procedure: 212 cases for Ch. 27 (208 plus 4 minimum cases).
- `StoryWindLoads.ComputeCases` gives every one of those cases on every floor, under its own name:
  - the wall part by the wall force on the floor's strip;
  - the minimum's 16 psf by wall area;
  - the roof and parapets on the roof.
- **Shear Wall Calculator:**
  - designs the walls for those exact cases;
  - saves only its hold-down couples, under those names, with no direction of its own;
  - keeps manual loads, which unlink the wind and never reach the Load Manager.
- **Load Manager:**
  - `RoofWind.Cases` gives one roof case per calculator case, with that case's own zones, GCpi sign and windward soffit (27.3.3 / 28.3.3);
  - its own 8 roof cases and the direction pairing are removed;
  - the hold-downs join by case name, and a stale saved case is left out with a note.

**Speed, timed on lbltwo:**
- **Load Manager uplift:** 2.9 s → 0.39 s per edit, with identical results (4,452 values compared).
  - Each point's zone is now found once per zone layout and shared by the cases with that layout.
  - The cases then run in parallel.
- **Shear Wall Calculator:** about 0.23 s per change, against 0.08 s with the old 36 cases. The wall analysis itself takes about 7 ms; building the 212 cases takes about 130 ms.

**Tests:** wind 336 pass (the 4 known published-example deviations fail as before), wind UI 65, SWC 219, Load Manager 50.

**Next:** build the wind cases' zone pieces once from the geometry, and fill in each case's values from them.

**Speed, second pass (2026-11-04), timed on lbltwo:**
- **Profile:** almost all of the 130 ms for building the 212 wind cases was in `WindLateralLoads.ProjectedAreas` (the wall and roof areas for the 16/8 psf minimum). It was a 2,000-point sum with about 70 roof-height evaluations at each point, about 32 ms a call, called 4 times. The zone overlays took about 1 ms.
- **Fix:** `ProjectedAreas` is now exact, using the trapezoid rule on the break points where the height changes slope. `ProjectedAreaTests` checks the flat, gable both ways and hip results by hand.
- **Zone overlays:** each corner wind's two zone maps are now overlaid once (`Overlap`, geometry only), and Cases 3 and 4 each put their own values on that geometry (`Resolve`).
- **Results:**
  - building the cases: 130 ms → about 10 ms;
  - the Shear Wall Calculator's wind step: about 200 ms → about 15–30 ms;
  - the Load Manager's whole chain: about 0.46 s per edit (3.0 s at the start), with identical uplift results (4,452 values).
- **Tests:** wind 340 pass (the 4 known deviations fail as before), wind UI 65, SWC 219, Load Manager 50.
