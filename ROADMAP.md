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
- [x] **Drawing walls on a DXF** (owner's question, 2026-11-07: plans rarely show centrelines; the owner asked for all three
  on 2026-11-09, with a way to turn off snapping to the plan when it gets cluttered: **Plan snap**, `SketchCanvas.SnapToPlan`,
  in the view bar, the right-click menu and Underlay ▾; off, nothing on the underlay snaps, the plan tools excepted).
  None tried on a real DXF yet. Suggestions:
  - [x] Draw by a face: the wall tool's option centreline / left face / right face; an exterior wall drawn along its
    outside face puts the centreline half the stud depth inside. (2026-11-09) **Wall: centreline ▾** in the view bar
    (`SketchCanvas.WallDrawnBy`, `WallsFromPlan.CentreOf`): left face = the wall lies to the right going along, so
    clockwise round the outside. In a run the corners are found again (`RunJoint`: the previous wall's end moved to the
    new one's centreline). The preview shows where the centreline goes. Sketch test.
  - [x] Snap between two lines: where the DXF shows a wall as two parallel lines 3.5 to 7.25 in apart, a snap to the
    line midway between them. (2026-11-09) `SnapModes.WallCentre`, on by default, ranked after the host's points:
    where two centres meet (a corner or a tee), a centre's end, else the nearest point on it; among the plan's own CAD
    lines only (`Snapper.WallCentres`, ± 1/4 in, parallel within 1°). Not tried on a real DXF yet. BM test.
  - [x] Walls from the DXF: pick a layer, find its pairs of parallel lines, propose walls on their centrelines to accept
    or reject one by one. (2026-11-09) Underlay ▾ > **Walls from the plan…** (the editor): the layer named for walls
    picked first; square walls only, the pieces on a line joined, each run on to where it meets another's centre (a
    corner, a tee), those drawn already left out (`WallsFromPlan.Propose`). On the plan the one under review is bold, the
    rest faint; Add it (A), Skip (S), Add all the rest; each wall its own undo step (`PlanWallReview`, `PlanWallsWindow`).
    Not done: walls at an angle; a wall broken by a door is proposed in pieces. BM and sketch tests, a snapshot.
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
  - [x] (2026-11-09) SWC's Pieces tab lists the whole wall's pieces, as the views draw it (`WallPanel.Framing`): the plates
    its full length, a stud at each of its own ends, the hold-down posts at the shear part's ends, the sheets over the part;
    the part named. Hub test: the south wall declared over 10 to 30 ft of 40.
  - [x] (2026-11-09) An undesigned shear wall (no wind yet) shows its declared sheathing (its card's faces,
    `ShearWallCheck.ShearDesigns`), no nailing until it is designed. Hub test: the construction window's view.
  - [x] (2026-11-09, owner: "a note needs to be added that the shear wall hasn't been designed… a dangerous step that
    could be missed") Not designed is said everywhere it shows: `ShearWallDesign.NotDesigned` (saved in `Lateral`), a red
    banner over every tab of the shared wall window (`WallPanel.NotDesignedBanner`, `NotDesignedWarning`) and NOT DESIGNED
    across the elevation, "SW NOT DESIGNED" on every app's plan, the construction window's heading and Pieces tab, the
    hub's shear-wall step never Done (Attention, the walls named) with each wall in the hub's mark (shown on opening SWC),
    and the Load Manager's foundation page in red (no forces from them). Hub tests.
  - [x] (2026-11-09, owner: "since walls and shear wall portions can be different length, how do you distinguish those?")
    The Pieces tab's framing table has a Where column: each piece in the whole wall (the plates), the shear wall (its
    studs, posts, blocking, from–to), or the plain wall past it; the studs split in and outside the part; the sheathing
    "over the shear wall only". The Elevation already dimensions the part ("shear wall 20'-0"") under the wall. The 3D
    tab (2026-11-09): a strip on the top plates along the part, whatever is toggled (blue; red not designed), said in its
    bar (`WallModel3D.PartCaption`, `WallPanel.PartNote`). WF test.
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
    - [x] (2026-11-09) The rim's and the roof blocking's splice strap picked from the catalog (`SpliceStrap`): the lightest
      strap tie of C-C-2026 p. 291 (the copy in docs/references) whose (160) tension takes the whole force and whose
      width fits the member's face less 1"; saved as `DiaphragmLine.Strap`, `DiaphragmChord.Strap`. Tests: 1,662 lb on a
      2x10 ST2215 (1,875), 301 lb HRS6 (605), 3,000 lb on a 2x4 MSTI36 (the 3" MSTC28 does not fit).
    - [x] (2026-11-09) Southern Pine 2x10 and 2x12 (Table 4B): from SFPA's Southern Pine Use Guide 2013 Table 1 (the June 1,
      2013 SPIB values; it matches the 2x4 to 2x8 rows read before), kept in NDS_WoodLibrary/docs/references. No. 2 2x10
      Fb 800, Ft 475, Fc 1300; 2x12 750, 450, 1250. The SP 2x10 rim by hand: 10,545 lb tension, 28,860 lb compression.
    - [x] (2026-11-09) A line partly over walls: the top plates take the collector over the walls under the line (within
      their length only: none past a wall's end, owner), the floor's rim past their ends, each checked for its
      own stretch's worst (both at the hand-off); the strap for the rim's. Hub test: a lower wall under 12 of 30 ft, N(t)
      recomputed by hand along the line.
    - [x] (2026-11-09) The roof's lines past the walls under them: the roof's continuous blocking between the rafters
      (owner's choice; `ModelDefaults.RoofBlockingSize`, 2x6 to start), checked as the rim is (`RimCollector`, member
      "blocking"), a strap across every joint for the whole force. Hub tests: the upper north wall shortened to 10..30 ft,
      v × 10 ft; the wall at x = 20 from 6 to 24 ft, v × 6 ft.
    - [x] (2026-11-09) A page to edit the Building Model's defaults: Building > Model defaults
      (`BuildingModel.Sketch.ModelDefaultsWindow`, one undo step): new stories, roofs and quick buildings, and the framing
      (studs, lumber, deflection limit, rafters, floor joists, roof blocking); a value not offered is kept.
    - [x] (2026-11-09) The diaphragm's field nailing zones (owner's choice: two per span): the shear falls linearly from
      each line to nothing at mid-span (a cantilever's free end), from the larger of the line's v and 0.6 w (L/2) / d; the
      line's nailing out to where v is under the lightest row's (rounded up to the 2 ft), the lightest row beyond
      (`DiaphragmChord.HeavyFromFt`, `HeavyToFt`, `LightNailing`). Hub test: 6d on 5/16" at 170 mph, 8 ft from each line
      (20 × (1 − 237.5 / 363.1) = 6.9 ft).
    - [x] (2026-11-09) The diaphragm's chords (owner's choices: simple spans between the lines of shear walls,
      M = w L² / 8, a cantilever w a² / 2 past the last line, w = the level's own wind (worst case) over its length;
      T = C = M / d, ASD): the edge walls' double top plates spliced as a collector (16d each side), the floor's rim where
      no wall is under the edge (a strap at each splice for the whole force). `DiaphragmLines.Chords`, saved as
      `Connections.Chords`; the construction window's Pieces tab. Hub tests: the box by hand (30 and 40 ft spans), and the
      rim with the lower north wall taken out.
    - [x] (2026-11-09) The roof's chord where no wall is under its edge: its continuous blocking, strapped at every joint.
    - [ ] The chords by the diaphragm's real moment (the walls' reactions per case, flexible or rigid) and the plates'
      splice layout (each splice placed, its force where it is), rather than the worst span's at every splice.
    - [x] (2026-11-08) The rim (a floor) or the roof's blocking toe-nailed into each shear wall's top plates
      (`WoodFraming.RimToPlate`, saved as `Connections.Tops`): the owner's choices, NDS Figure 12A's geometry for the yield
      equations (ts = (L/3)/cos 30° in the rim, the rest into the plate; the NDS gives no rule) and the wall's own ASD shear
      over its shear part (conservative); Z' = Z × 1.6 × Ctn 0.83 (12.5.4.2); the starting toe-nail 8d
      (`ConnectionDefaults.ToeNail`, in the defaults window). The top pair's words name it; the construction window and
      the wall elevation show it.
    - [x] (2026-11-09) Framing plates where the toe-nails need closer than 3" (`TopClip`, `ShearWallCheck.TopClipOf`, saved
      on `TopConnection.Clip`): LTP4, C-C-2026 p. 310 configuration 7 (top plates to rim), direction G, (160) DF/SP 715,
      SPF/HF 615 lb (`FramingPlates_Manager`); s = 12 A / v to the inch below, 24" at most, 6" at least. On a face with no
      wood panel (gypsum goes on after) with 0.131 x 1-1/2 nails; both faces paneled: over the thinner, up to 1/2", with
      0.131 x 2-1/2 nails at the full load (footnote 3). The program's placement; tell it otherwise if you place them differently.
    - [x] (2026-11-09) The horizontal pairs drawn on the plans (teal, `ShearPairs.Drawn`, the hold-downs' WISE rule; the
      worst case of each half, or the picked diaphragm's case) and a diaphragm's free body: pick it on its level (the shared
      sketch, `ShearPairs.DiaphragmKey` in the body; the Building Model, SWC and the Load Manager's Foundation page, which now
      steps up through the levels): its own wind and each wall's half drawn in one case (`SketchCanvas.FreeBodyCase`), the
      Load Manager's side panel listing them with ΣFx, ΣFy, ΣMz (`ConnectionBalance.DiaphragmForces`) and a case picker.
      LM test by hand: 1000 − 2000 + 1000 = 0.
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
  - [x] (2026-11-09) Open an app on a given page: each step has its page (`WorkflowStep.Page`), the hub passes it
    (`Launcher.ArgumentsFor`: `--page foundation`, with `--select`), the Load Manager opens on it (`MainWindow.ShowPage`:
    loads, foundation, foundation3d).
  - [x] Keeps the project in spec (2026-11-06): the stamps on the foundation forces (`Lateral.BasedOn`, by
    `ModelFingerprint`), the shear walls designed again with no window in the background (`ShearWallCheck.Run`), a red
    "Fails" step, a problems banner with "Show W4" (opens SWC at the wall, `--select`), and the problems marked in the file
    (`Hub.Status`: since when, seen by whom). SWC watches the file the whole time and names the walls that newly fail.
  - [x] One person per project; the rest look only (2026-11-06): the hub's own lock, `ProjectLock.HeldByOthers`; the apps
    refuse a project someone else has, and the hub opens it view only (no apps, no writes).
  - [x] **View-only mode for the apps** (the owner's idea, 2026-11-06; done 2026-11-09 in the Building Model, SWC and the
    Load Manager): someone else's project is offered "Open it view only?" (`ProjectDialogs.TakeLock(..., offerViewOnly)`,
    no lock taken); then no saves, no "save first?", a banner and "(view only)" in the title. The sketch puts back every
    edit, the geometry and the apps' data alike (`SketchCanvas.ViewOnly`, `EditRefused`), and `SketchView.ViewOnly` hides
    the tools that change anything (Select and Measure stay; set back for a project of one's own). The Building Model's
    editing menus are off and its properties panel too; SWC reads the calculators again after a refused card edit and
    opens no Edit building, wind or loads; the Load Manager's load edits are put back and its side panel's boxes read
    only. SWC and the Load Manager read the other person's saves as they make them.
  - [x] View only, the rest (2026-11-09): the wind calculator (its Results page only, `WindSetupControl.ReadOnly`; their
    saves read again whole); the hub's view only opens the apps view only (`--view-only`, `ProjectLock.ViewOnlyArgument`,
    no second question; "Look in the … (view only)"); the Load Manager's floor and roof loads window read only
    (`AreaLoadsWindow(b, readOnly)`); SWC's Loads… and Connection defaults not opened.
  - [x] Every app's calculation re-run with no window, in order (2026-11-06): `WindCheck` (the wind calculator's),
    `ShearWallCheck` on that wind, the hold-downs now against saved (by how much), `FoundationCheck` (the Load Manager's)
    on the forces as they are now; `ProjectCheck` chains them. The wind app and the Load Manager watch the file too, and
    the Load Manager says when the saved shear wall forces are older than the building or the wind. Uplift on the
    foundation is a flag for the foundation design (amber), not a fault.
  - [x] (2026-11-09) Show the hub's problems in the apps too: the format moved to BuildingModel.Core (`ProjectStatus`,
    `StatusItem`; the hub makes it, `HubStatus.From`); SWC shows the Shear walls step's items on opening, the Load Manager
    its two steps' (`ProjectStatus.Words`), each with since when and whose hub saw it.
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
  with the design. (Looked at 2026-11-09, left as it is: three things are outside the history, not one. The typed story
  loads reach the building only at save (`ShearWallProject.Save`). The manual-or-wind switch (`windLoadsLinked`) changes
  what a save writes, `Lateral` included, and lives only in the window. And the calculators read the typed loads only
  on opening, so an undo would not put them back. First: keep the loads and the mode in the "ShearWall.Project" section
  as they change, each an undo step, applied again after an undo. Until then the text compare is correct.)
- [ ] **More than one shear part on a wall**, and a part per story (today one part, the same on every story the wall
  spans). For now, split the wall in the Building Model.
- [x] **Show the declared shear walls in the Building Model** (2026-11-09): the shared sketch reads the `Lateral` section's
  walls (every declared wall, designed or not) and draws a blue band on each shear part, tagged SW (SW fails in red)
  (`SketchCanvas.ShowShearParts`, on by default; SWC turns it off, it draws its own).
- [ ] **A suggested shear part:** the full-height stretches between doors and windows, offered when a wall is declared
  (the model has no wall openings yet: its openings cut floors).
- [ ] **Relax the hub's one-app-at-a-time rule** now that saves merge (for example, allow the Load Manager beside the
  Shear Wall Calculator), and let each app warn from the locks instead.
- [x] **SWC clean-up after the geometry lock** (2026-11-09): the dead handlers removed (Export/Apply change set, Copy from
  below, Move, Copy, Note, Delete; nothing wired them); the Members tab shows the coordinates read-only, with no delete
  buttons (`GeometryPanel.GeometryLocked`); the construction window already shows and picks the stud size. Hub test.
- [x] **SWC to watch the project file always** (done with the hub's checks, 2026-11-06: `WatchProject` from the open on;
  ticked 2026-11-09).
- [x] **Stamp stored results with what they were based on** (model revision, inputs hash), as `Lateral` does with
  `ModelRevision`, so the hub can say exactly what is out of date. (2026-11-09) The only results stored are SWC's
  `Lateral` and `Connections` (one save; the foundation loads are worked out when shown). `Lateral.BasedOn` already had
  the building, the wind and the walls' inputs; now also the connection defaults (`ShearWallCheck.ConnectionDefaultsPart`).
  A part an older stamp lacks is unknown, not changed (`ModelFingerprint.Changed`). Every app's views now read the stamp
  too, through one routine (`LateralSection.OutOfDate`: a wall gone or moved, or the building or the wind changed): the
  Building Model's wall window and plans said nothing when only the wind or the building changed. The plans cache it per
  model version. SWC's fallback to the saved section leaves the wind out (its building holds the wind aside:
  `SavedLateral`). Tests: WF, hub.

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
- [ ] Found while making the tutorials (2026-11-10):
  - The Calculations tab's hint still says "Draw the shear walls: click Wall, then click each wall's start and end"; the
    geometry is the Building Model's now, so it should say to pick a wall and **Make it a shear wall**.
  - **Make it a shear wall** (and picking a wall upstairs just after) opens the construction window even with "Open when
    a wall, beam or column is picked" cleared.
  - The Building Model's DXF underlay draws the lines and arcs only: a plan's TEXT (room names, labels) is not drawn.
  - A roof (or floor) nailing the SDPWS 4.2 tables do not have (10d with 7/16" roof sheathing, blocked) shows on a wall's
    Pieces tab as "Nailing (not designed): 0.0 plf ≥ 63.4 plf (0.00)": it reads like a pass. It should say NOT DESIGNED in
    red everywhere (and be an issue), and Connection defaults could warn when the nail and panel do not go together.
  - The construction window's chord table ("Diaphragm chord (at the top)": Span, Moment, Chord force, Chord, Field nailing
    zones) lays out badly: the auto-width columns take the width, the last (star) column gets almost none, its text wraps a
    letter a line and the row grows very tall (`WallBuildWindow.Table`).
  - After File > Save (shear parts set, two posts added in the Building Model), the plan's hold-down labels still say
    "(out of date)" and the construction window says "The lateral design is out of date: W1 east end: wall W1 has moved
    (13.5 ft from its end)". W1's shear part is 0 to 14 of its 60 ft. Cause: `LateralSection.Stale` (BuildingModel.Core)
    measures each saved point against the wall's Start and End only, so a shear part's end partway along the wall is
    always "moved". It should measure against the shear part's ends (or the point's distance to the wall's line). The
    Load Manager and the hub repeat it, and the hub marks Foundation loads **Fails**.
    **Done 2026-11-10 (BuildingModel 6a37744):** measured from the shear part's ends as saved (`Walls`), and the saved
    part must still lie on the wall; an older save with no `Walls` as before. `LateralStaleTests`.
  - The post (column C2) put under an upstairs hold-down that lands partway along a wall below: "does not balance the
    wind: ΣM = 294 lb·ft in Case 1, from W, roof Case A, +GCpi (rigid) (of 35,260)" in the Load Manager and the hub.
    **Done 2026-11-10 (LoadManager 5c8f6a9):** the cause was W5's east end pressing at its end post's centreline (x 15.875)
    on the post at the hold-down (x 15.75): 2,350.6 lb × 0.125 ft. A column's free body now takes the wind's connections
    on its axis, as its gravity loads already were (`FreeBodyConnection.Axis`); the wall's own free body keeps its post's
    centreline. The example: 13 of 13 members balance, the hub all Done.

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
- [x] C&C (Ch. 30), 2026-11-09: h is the eave height for θ ≤ 10° (Figs. 30.3-1 and 30.3-2A to 2I, 7-16 pp. 335-337
  and 7-22 pp. 317-321 read: "h = Mean roof height, except that eave height shall be used for θ ≤ 10°"), for qh and
  the zone widths (`BuildingData.CcFigureHeight`, the MWFRS figures' rule); the walls' "a" ≤ 0.8h for θ ≤ 7° and a least
  dimension over 300 ft (Fig. 30.3-1, walls only: `AreaCalculator_CC_Base.IsWallFigure`). The wind's details page
  shows the figure height and finds Kh and qh at it, as the calculators do. Tests: `CcFigureHeightTests`.
- [x] C&C, 7-16 hip roofs 7° < θ ≤ 20° (Figs. 30.3-2E/2F, 7-16 p. 340 note 7), 2026-11-09: each zone at its own h/B,
  B the dimension normal to the wind (26.3; the owner, 2026-11-09: h/B goes by the wind direction). Zone 3: the least
  dimension (note 7); Zones 1 and 2e: the eave they lie along, wind normal to it, B that eave's length; Zone 2r (not in
  note 7): the larger suction of the two directions (`WindLoadCalculator_CC.HipRatiosFor`, `ApplyHipHOverBByZone`,
  the overhangs too). `BuildingData.HipCladdingHOverB` is Zone 3's, the figure the charts show. Was one h/B from the
  least dimension for the whole roof (wrong: it is not per direction).
- [x] MWFRS L/B and h/L per wind direction, 2026-11-09 (the owner asked): already so, each direction's calculator on
  the building turned so L is along the wind (`WindAnalysis.Run`); now tested (`Mwfrs_LeewardWallCp_ByEachWindDirectionsLOverB`).
  The wind's Building page showed one L/B and h/L as if for every wind: now each direction's L, B and L/B.
- [x] The wind's MWFRS pages: a hand calculation of the wall pressures (owner, 2026-11-09): that direction's L and B,
  V, α, zg, Kz, Kh, qh with the numbers, G, GCpi, each Fig. 27.3-1 Cp (the leeward's interpolation by L/B), qz up the
  windward wall (Table 26.10-1's heights, qz = qh at h), qh on the leeward and side walls, both signs of GCpi, and
  windward + leeward. Printed in the report too (`WindReportTests.MwfrsPages_HandCalculation_ThisDirectionsLAndB`).
- [ ] 7-22 hip roofs θ ≤ 7° with overhangs: Table 30.7-1 sends hip roofs to roof + wall GCp; the code
  uses the Fig. 30.3-2A overhang curves (as for gables). Decide which applies.

## Done

- [x] 2026-11-05: ASCE 7-16 audit of Ch. 26 (the parts used), Ch. 27 (Directional) and Ch. 28
  (Envelope) against the code PDF. The values matched; the Ch. 28 section references now follow
  the edition (7-16 Fig. 28.3-1 notes 3, 5 and 8; 7-22 28.3.2 / 28.3.2.1).
