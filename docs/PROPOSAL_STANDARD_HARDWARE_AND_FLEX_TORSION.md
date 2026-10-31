# Proposal: company standard hold-downs, and torsion with a flexible diaphragm

_Branch `dev/wind-core-refactor`. Written 2026-10-30 before the work, per the user's request ("propose the design, then do it")._

## 1. Company standard hold-down list ("favorites")

**Decisions from the user:** the list belongs to the app, not to a project. It is the company's minimum standard hardware.

### Data
- Each catalog option (one model with one fastener option) gets a stable key `Model#n`, n = the option's position (1-based) in that model's `FastenerOptions` in `NDSProvisions/SimpsonCatalog`. Fastener text alone is not unique (HDU14-SDS2.5 has two "36 1/4 SDS, 3.5 × 5.5" rows).
- `HoldownStandardList` (core, `Design`): the set of keys. Load/save as JSON. The app keeps it in one file for the whole app: `%AppData%\ShearWallCalculator\HoldownStandardList.json`. Tests use a temporary path. Keys no longer in the catalog are kept in the file but reported as "not in the catalog".
- `DesignSettings.HoldownsFromStandardList` (bool, saved with the project, default off). The project records which pool its hold-downs came from; the list itself stays in the app.

### Selection
- `ShearWallDesigner.SelectHoldown(family, species, required, standardOnly)`: the lightest option of the wall's family with allowable ≥ required, drawn from the standard list when the toggle is on, else from the full catalog.
- With the toggle on and no standard option strong enough, the wall gets no hold-down and the check says so ("no standard-list HDU reaches 6,200 lb"). It never falls back to the full catalog without telling the engineer.
- The selected option carries its key and minimum wood member, and its text names the post size, e.g. "HDU14-SDS2.5 (36 1/4 SDS, min. 3.5 × 7.25)", so rows with the same fasteners are told apart.

### Screens
- **Hold-down catalog (View menu):** a "Standard" checkbox on every row edits the list. Each click saves the file and the design re-runs at once. Filters: family, model text, "only the standard list", "only used by the design".
- **Design tab:** a toggle in the settings bar, "Hold-downs: Standard list / Full catalog". Clicking it re-runs every wall check. The bar says how many options are in the list ("Standard list: 7 options").
- **Report:** says which pool was used, and lists the standard options.

### Company default shipped with the app (built 2026-10-31, user chose "Option A plus the version record")
- **Packaged file:** `ShearWallVisualizer/HoldownStandardList.default.json` (copied next to the exe on build). It has `Version`, `Date` and `Keys`. It starts at version 0, empty.
- **Each user's copy** (`%AppData%\ShearWallCalculator\HoldownStandardList.json`) records the company version it came from and whether the user has changed it (`Customized`).
- **At start-up** (`HoldownStandardList.Startup`):
  - No user copy yet: the user copy is created from the company default.
  - The company version is newer and the user has not changed their copy (or it already matches): the copy is replaced automatically.
  - The company version is newer and the user has edits: a one-time question. **Yes** replaces their list with the company list. **No** keeps their list, and they are not asked again for that version.
- **New projects** start with "Standard list" on whenever the list has any options. Older projects keep what they saved.
- **Catalog window:**
  - A "Company" column marks the options in the company default.
  - **Restore company default** replaces the user's list.
  - **Save as company default...** writes the next version.
  - The footer says how the user's list differs from the company default.

**How to change the company list later:**
1. Run the app, open View > Hold-down catalog, and tick the options you want.
2. Click **Save as company default...** and save over `ShearWallVisualizer\HoldownStandardList.default.json` in the source. The dialog opens there when the app runs from a build inside the source tree. The version number goes up by one and today's date is stamped.
3. Commit the file and rebuild. Each user gets the new list the next time they start the app, or is asked if they have changed their own.

**Version record in the project** (`DesignSettings.HoldownList`, written on every save): which pool was used, the company list version and date, whether it was the user's own edit, and each wall's hold-down key. On opening, walls whose hold-down is now different are listed (e.g. "Wall 3: HDU5-SDS2.5#1 → HDU8-SDS2.5#1") with both list versions. The project then counts as changed: save to keep the new design, or close without saving to leave the file as it was. Older projects have no record and are not checked. The report states the list version used.

## 2. Torsion with a flexible diaphragm

**Basis (from the user's notes):** a flexible diaphragm gives each wall line its direct force by tributary area (simple spans between lines), not by wall stiffness. Torsion is not distributed by the rigid-diaphragm stiffness equation. One published approach (WoodWorks, *Torsional analysis for flexible diaphragms*) builds **equivalent distribution weights** that reproduce the flexible direct distribution, then distributes the torsional moment with them:

- Direct: F_di = V × (tributary share of line i).
- Equivalent weight: K_i = F_di / V = the line's tributary share (dimensionless; Σ K_i = 1 in each direction).
- Center of the equivalent system: x_c = Σ K_i x_i over the N-S lines, y_c = Σ K_i y_i over the E-W lines. For a uniform load this is the load's resultant, so the direct forces carry V with no extra moment.
- J = Σ(E-W lines) K_i ȳ_i² + Σ(N-S lines) K_i x̄_i², with x̄, ȳ measured from (x_c, y_c).
- Torsional force on line i (force on the diaphragm, CCW-positive torsion T): E-W lines +K_i T ȳ_i / J; N-S lines −K_i T x̄_i / J. These are the same signs as the rigid calculation. They sum to zero in x and in y, and their moment about (x_c, y_c) is −T.
- T is the load case's own torsion Mt (Fig. 27.3-8 Cases 2 and 4, Ch. 28 torsional cases and corner asymmetry). The direct tributary distribution already places V. Each line's force is split among its walls by rigidity, as now.

**Assumption to state on the sheet:** the weights for the x and y lines are each normalized to 1 in their own direction. This sets how T is shared between the x and y lines. It follows the WoodWorks formulation, and it is an assumption: a semi-rigid model would be needed to do better.

**Checks printed for every case:** Σ reactions = −V in each direction; torsional forces sum to zero in x and in y; their moment about the center = −T.

## 3. Chapter 28 zones on the walls (user, 7-16 and 7-22)

- Wind normal to the ridge (7-16 Case A; 7-22 Load Cases 1 and 3): zones only on the walls parallel to the ridge (1, 1E, 1T and 4, 4E, 4T). No zones on the gable end walls. This is what W7 does.
- Wind along the ridge (7-16 Case B; 7-22 Load Cases 2 and 4): zones on all four walls (5/5E/5T, 6/6E/6T, 1/1E, 4/4E). This is what W7 does.
- Case B values confirmed (7-16): 2 = −0.69, 3 = −0.37, 1E = 4E = −0.48, 2E = −1.07, 3E = −0.53.
- Still assumed: in the Case B torsional case only 5T/6T are reduced (not the side walls or the roof).
