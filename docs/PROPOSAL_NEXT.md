# Proposal: Project files + report (1), shear-wall design (2), and the smaller items (3)

_2026-10-30. Branch `dev/wind-core-refactor` in both repos. Items 1 and 2 are approved. Item 3 is a recommendation for you to decide on._

---

## 1. Save everything in one project file, plus a printable report (roadmap M7)

### What happens today
- **File > Save** only writes the shear-wall calculator to JSON: walls, diaphragms and the loads V_x/V_y (`JsonDrawingSerializer.Save`).
- **None of the wind inputs are saved.** That means the edition, risk category, V, exposure, Kzt, enclosure, building dimensions, roof and the component choices.
- **File > Open** unlinks the wind loads and restores the old saved V_x/V_y. Wind Setup goes back to its defaults, so you re-type the site and building. The two can disagree without anything telling you.
- `OpenFile` (the Recent files list) is still a placeholder message box.

### What I would do
1. **One versioned project file (`*.swc.json`)**:
   ```
   { "FormatVersion": 2,
     "Wind":   { "Edition", "Parameters" (WindParameters_Base), "Building" (BuildingData),
                 "OnlyCases1And3", "Linked": true/false },
     "Walls":  { the current calculator JSON, unchanged },
     "Loads":  { "Vx", "Vy", "Source": "ASCE7" | "Manual" } }
   ```
2. **Opening a file**:
   - Restores Wind Setup exactly, recalculates, and re-links the loads if they were linked when saved.
   - If the recalculated wind load differs from the saved one by more than 0.5% (for example because the engine was fixed since), it says so in one line instead of silently changing.
   - **Old files** (no `FormatVersion`) still open the way they do now: walls plus manual loads, with a note that the wind inputs were not in the file.
3. **Save / Save As / Recent files** work properly. The title bar shows the file name and a `*` when there are unsaved changes, and you are asked before closing with unsaved changes.
4. **Printable report** (Print… and Save as PDF through the Windows "Microsoft Print to PDF" printer, so nothing is downloaded):
   - It reuses the Detailed calculations pages as a WPF `FlowDocument`, so the report and the screen can never disagree.
   - **Contents:**
     - cover (project, date, edition, engineer field);
     - inputs and assumptions;
     - velocity pressure;
     - cladding zones and graphs;
     - MWFRS for each direction;
     - load cases;
     - shear-wall results (and the design checks from item 2 once they exist).
   - Every equation line carries its ASCE 7 or SDPWS section number.
   - Page headers and footers show the file name, page N of M, and the engine commit.
5. **Tests**:
   - a round trip (save, open, every input identical, same loads);
   - an old-format file opens;
   - the "loads changed since saved" message appears;
   - every report page builds.


### Effect on the app
- New menu items: Save As and Print report…. File > Save and File > Open keep their places.
- There are no changes to the wind screens.
- Old drawings still open.

**Estimate:** about 2–3 days.

---

## 2. Shear-wall design (roadmap milestone, previously "#5")

### What already exists (found in this repo)
| Piece | Where | State |
|---|---|---|
| SDPWS Table 4.3A/B/C nominal unit shear (wind column v_w, seismic v_s, G_a) | `NDSProvisions/Table4_3*.cs` | Data entered; not used by the app |
| SDPWS Table 4.2A/B/C (diaphragms) | `NDSProvisions/Table4_2*.cs` | Data entered; not used |
| Aspect-ratio limits (4.3.4) | `SDPWS_Table4_3_4.cs` | Present; the 2w/h reduction above 2:1 is not |
| Simpson HDU (9), HTT (6), strap ties (45), DF-SP and SPF-HF allowable loads | `NDSProvisions/SimpsonCatalog/` | Data entered |
| `ShearWallSelector` | `ShearWallSelector.cs` | Prototype. **Bug:** it chooses connectors against the *shear* V, not the holdown tension. It is only reachable through the Test Design button and only prints to the console |
| `WallDesignControl` | `ShearWallVisualizer/Controls` | Stub UI |

### What I would do
1. **Per-wall design inputs** (with sensible defaults so a non-engineer can just press Check):
   - sheathing type and thickness;
   - nail size and edge spacing (6/4/3/2 in.);
   - blocked or unblocked;
   - one or both sides;
   - species group (DF-SP / SPF-HF);
   - dead load on the wall (plf), optional;
   - holdown family (HDU / HTT / strap).
2. **Checks for each wall and each direction**, using the governing load case from the wind workspace. Each check is listed with its code reference.

   | Check | Equation | Reference |
   |---|---|---|
   | Unit shear | v = V / ΣL (segmented walls) | SDPWS 4.3.2 |
   | Aspect ratio | h/b ≤ 3.5 (blocked wood panels); × (1.25 − 0.125 h/b) above 2:1 | SDPWS 4.3.4 / 4.3.3.4 |
   | Capacity, **ASD or LRFD (your choice per project)** | ASD: v_w / 2.0; LRFD: 0.80 × v_w (the tables hold nominal values) | SDPWS 2015 4.3.3 / 2021 4.1.4 |
   | Holdown tension | T = M / L = V·h / L: the moment from the shear at the top of the wall over the wall length. ASD uses 0.6W, LRFD 1.0W. Dead-load relief (0.6D) is an optional input that defaults to 0 | your rule; ASCE 7 2.3/2.4 combinations |
   | Holdown pick | the lightest HDU / HTT / strap model with allowable tension ≥ T (ASD), for the chosen species and use (wall to foundation, or wall to wall below) | Simpson catalog values (C_D = 1.6 already included). For LRFD, T is compared with the allowable × 1.6/1.0 conversion only if you approve; otherwise holdowns are always checked at ASD |
   | Sill anchorage | V / bolt capacity → bolt spacing (later, if you want it) | NDS 12 |
3. **Results**:
   - A pass/fail strip on each wall in the drawing (green / amber / red).
   - A per-wall table with demand, capacity, ratio, the chosen nailing and the holdown model.
   - An **Auto-select** button that picks the lightest nailing and holdown that pass.
   - The same checks go into the item 1 report.
4. **Tests**:
   - hand calculations against SDPWS for unit shear, the aspect factor and ASD/LRFD;
   - holdown tension with and without dead load;
   - holdown selection boundaries (just under / just over a catalog value);
   - the selector bug fix.

### Your answers (2026-10-30)
- **Q1:** offer **both ASD and LRFD** as a per-project choice.
- **Q2:** the HDU / HTT / strap-tie data is for **holdowns at the base of walls**, to the foundation or to the wall below. There is no roof-tie data yet, so **roof ties (H-clips / hurricane ties) are a future milestone** (goes with item 3b).
- **Q3:** not sure. Researched: the tables are **SDPWS 2015**, and their wind column equals the 2021 v_n, so both editions are supported for wind. The evidence is in `docs/SDPWS_EDITION.md`, the code is `NDSProvisions/SdpwsSource.cs`, and every table file has a source header.
- **Q4:** **rigid diaphragms first**, then flexible.
- **Holdown rule (yours):** T = M / L, where M is the moment from the shear at the top of the wall. The existing `ShearWallSelector.ComputeHolddownForces` already computes V·h/L. The bug is only that the selection used V instead of T.

### Effect on the app
- In 2 Shear walls → Calculations, every wall row gains demand / capacity / ratio and holdown columns.
- A wall-properties panel opens when you select a wall.
- The drawing shows pass/fail colours.
- The Test Design button goes away, replaced by Check design / Auto-select.

**Estimate:** about 4–6 days (rigid), plus 1–2 days to extend to flexible.

---

## 3. Smaller items: engineering benefit and effect on the app

| Item | Engineering benefit | Effect on the app | Size |
|---|---|---|---|
| **a. Actual framing spacing input** (EWA = span × max(spacing, span/3)) | Today framing always uses span/3, which is right for most rafters and studs because span/3 is usually more than 16–24 in. It only changes the answer for **short spans with wide spacing**, for example 6 ft purlins at 4 ft o.c. (EWA 24 vs 12 sq ft, about 5–10% less suction). Small benefit, mostly completeness. | One "spacing" box appears next to the span when Rafters or Studs is chosen. | ½ day |
| **b. Roof overhang C&C and MWFRS overhang uplift** (N3) | Overhangs see the **highest suctions on the building**: the soffit is pressurised and the top is in suction, the overhang GCp curves are more negative than the matching roof zone (exact values to be read from Fig. 30.3-2 and checked by you). They control **rafter-tail and ridge connections and roof-to-wall ties** (H-clips/hurricane ties), a common failure point in real storms. Without them, the report has a gap an engineer reviewer will notice. | An "Overhang width" field on the Building page, overhang zones shown in the 3D viewer, an extra row group on the cladding page, and an uplift-per-tie line. Pairs naturally with item 2 if you have roof-tie data. | 2–3 days |
| **c. Chapter 28 low-rise envelope procedure** (N1) | For buildings with h ≤ 60 ft and h ≤ the least plan dimension, Ch. 28 is the procedure most engineers use for wood-frame shear walls. It often gives **lower base shear** than Ch. 27 for low-slope roofs (I will quantify this on your test buildings rather than quote a number), and its **torsional load cases** (Fig. 28.3-1) check walls for the corner pattern directly. Offering both lets you pick the governing or the economical one, with the reason shown. | A "Procedure: Directional (Ch. 27) / Envelope (Ch. 28)" choice on the Building page, defaulting to Ch. 27 so nothing changes unless you choose it. New GCpf figure data, tests, and a Detailed calculations page. The shear-wall link uses whichever is selected, and the report says which. This is the largest of the four and needs the same figure-data verification by you as Ch. 27 did. | 4–5 days |
| **d. Parapets** (deferred by you) | MWFRS parapet load (GCpn +1.5 / −1.0) adds to the shear-wall base shear, and C&C parapet pressures apply. It matters only if there are parapets. | A parapet height field and an extra force line. | 1–2 days |

**My recommendation for item 3:**
- Do **(b) overhangs** right after item 2. It has the highest safety value, and it uses the same connector data.
- Do **(c) Ch. 28** as its own milestone once item 2 is stable, because it changes the loads the walls are designed for and deserves its own review.
- Do (a) and (d) when convenient.

---

## Order of work
1. Item 1 (project file and report). It needs no decisions; I start on your go.
2. Item 2 (shear-wall design), rigid diaphragm first, then flexible.
3. Item 3b (overhangs) together with a new roof-tie milestone, then 3c, then 3a and 3d, if you approve them.

Each step ends the same way: a clean commit in each repo, both solutions building, all tests passing, and an entry in `PROGRESS.md`.
