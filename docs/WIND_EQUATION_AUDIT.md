# Wind Equation Audit (ASCE 7-16 / 7-22)

_2026-10-30. Engine: `ASCE7-Wind-Load-Calculator/ASCE7Wind.Core` @ `daa404c`. Companion to `DATA_VERIFICATION.md`
(figure data) and the "Needs your engineering confirmation" list in `PROGRESS.md`._

This audit covers the **equations and how they are applied**. It does not re-check the digitized GCp/Cp curve
values; `DATA_VERIFICATION.md` covers those. Each item below was checked against the ASCE 7 provision named, and
against published secondary sources where they exist (listed at the end). I did not have the printed standard,
so items marked **confirm** need a check against your copy.

Example used for the numbers: 60 × 40 ft, 15 ft eave, 22° gable, ASCE 7-22, V = 115 mph, Exposure C, enclosed.
That gives qh = 30.25 psf and Kd = 0.85.

## Findings that change results

### F1. HIGH — FIXED (W7 73adc56): C&C GCp is read at the *whole zone's* area, not the component's effective wind area

`WindLoadCalculator_CC.CalculateExternalPressures` evaluates each GCp curve at `a => a.Area`. That is the area
of the zone polygon (for example, all of roof Zone 1 = 832 sq ft).

- **What ASCE 7 says (26.2):** the *effective wind area* is the area of the **component** being designed. It is
  the span times an effective width that need not be less than one-third of the span. For cladding fasteners,
  it is no more than the area tributary to one fastener.
- **Why it matters:** GCp gets smaller in magnitude as area grows, so using the zone area is **unconservative**
  for every real component (fasteners, sheathing, studs, rafters, windows).

| Zone (example) | Zone area used | GCp used | Design suction now | At 10 sq ft (typical fastener/sheathing) |
|---|---|---|---|---|
| Roof 1 | 832 sq ft | −0.80 | −25.2 psf | about −1.5 → **−43.2 psf** |
| Roof 2 | 64 sq ft | −1.46 | −42.1 psf | about −2.5 → **−68.9 psf** |
| Roof 3 | 16 sq ft | −2.67 | −73.4 psf | about −3.0 → **−81.8 psf** |
| Wall 5 | 60 sq ft | −1.12 | −33.5 psf | about −1.4 → **−40.6 psf** |

The headline "Roof uplift −73.4 psf" on the Results page would be about −82 psf for roofing fasteners.

**Proposed fix (needs your decision):** add a "component" input with an effective wind area, with presets such
as *roof sheathing / fasteners 10 sq ft*, *rafters/trusses span × span/3*, *studs*, *windows (actual size)*.
Then evaluate GCp at that area in every zone. The zone polygons stay as they are; they only decide *where* each
GCp applies. Until this is fixed, the safe reading is the value at the left (small-area) end of each curve.

### F2. HIGH — FIXED (W7 73adc56): the 16 psf C&C minimum (30.2.2) is not applied

The 16 psf minimum is applied to the MWFRS (27.1.5, 16 psf walls / 8 psf roof), but not to cladding. Section
30.2.2 requires a C&C net design pressure of at least 16 psf **acting in either direction**. Example: roof
Zone 1 push-in is reported as 12.3 psf and should be 16 psf. (Like all strength-level loads in 7-16/7-22,
this is 16 psf ultimate, or 9.6 psf at ASD.)

**Proposed fix:** clamp each C&C net pressure to at least +16 / −16 psf, and show "minimum governs" the same
way the MWFRS cards do. It is a small engine change plus tests; it can go in with F1 or on its own.

### F3. CLOSED: the Kz floor for low buildings in Exposure B

The code takes Kz at z ≥ 15 ft in all exposures (Kz = 0.57 at 15 ft in Exposure B, 7-22). Some 7-22 references
list a minimum height of 30 ft for Exposure B (and 15 ft for C, 7 ft for D) and hold Kz there.

- If that applies to Kz, Exposure B buildings under 30 ft get **about 20% more** qh (Kz 0.70 instead of 0.57).
- I believe those heights are the gust-factor parameter z_min from Table 26.11-1, not a Kz floor.
- ASCE 7-10 did have a "z not less than 30 ft in Exposure B for C&C" rule.

This is the same open question as "Exposure B below 30 ft for C&C" in `PROGRESS.md`. Please check Table 26.10-1
and its footnotes in your copy.

## Checked and correct

| Item | Code | ASCE 7 | Status |
|---|---|---|---|
| Velocity pressure, 7-16 | qz = 0.00256 Kz Kzt Kd Ke V² | Eq. 26.10-1 (7-16) | ✓ |
| Velocity pressure, 7-22 | qz = 0.00256 Kz Kzt Ke V², with Kd in the pressure equations | Eq. 26.10-1 (7-22); Kd moved to Eqs. 27.3-1, 30.3-1 | ✓ (matches the published 7-22 change summaries) |
| Kz form | Kz = C (z/zg)^(2/α), z not less than 15 ft; C = 2.01 (7-16), 2.41 (7-22) | Table 26.10-1 | ✓ (see F3) |
| Terrain constants 7-16 | B 7.0/1200, C 9.5/900, D 11.5/700 | Table 26.11-1 | ✓ |
| Terrain constants 7-22 | B 7.5/3280, C 9.8/2460, D 11.5/1935 | Table 26.11-1 (7-22) | ✓, **confirm**: reproduces published 7-22 Kz at 30 ft (C 0.98, D 1.16/1.17; B 0.69 vs. 0.70 published) and Kz = 0.57/0.85/1.03 at 15 ft |
| Kd | 0.85 (buildings, MWFRS and C&C) | Table 26.6-1 | ✓ |
| Ke | 1.0 | 26.9: 1.0 permitted at any elevation | ✓, conservative at high-elevation sites |
| Kzt | user input, 1.0 on flat ground | 26.8 | ✓ (no Fig. 26.8-1 calculator yet) |
| G | 0.85 | 26.11.1, rigid building | ✓ for low-rise; no natural-frequency check (fine for h ≤ 60 ft) |
| GCpi | ±0.18 enclosed and partially open, ±0.55 partially enclosed, 0 open | Table 26.13-1 | ✓ (the Ri reduction for large partially enclosed volumes, 26.13.1.1, is not applied; conservative) |
| C&C pressure | p = qh [(GCp) − (±GCpi)], × Kd for 7-22 | Eq. 30.3-1 | ✓ equation (see F1 and F2 for how it is applied) |
| C&C zone width a | min(0.1 × least dimension, 0.4h), not less than max(0.04 × least dimension, 3 ft) | Fig. 30.3-2 notes | ✓ |
| C&C flat roof zones | 0.6h bands, 0.2h corners | Fig. 30.3-2A (7-16/7-22) | ✓ |
| C&C height limit | Part 1 only for h ≤ 60 ft; flagged above | 30.3 | ✓ |
| MWFRS pressure | p = q G Cp − qi (GCpi), × Kd for 7-22; windward wall uses qz by height, others qh | Eq. 27.3-1 | ✓ |
| MWFRS wall Cp | windward 0.8, side −0.7, leeward −0.5 / −0.3 / −0.2 by L/B (linear between) | Fig. 27.3-1 | ✓ |
| MWFRS roof Cp | Fig. 27.3-1 by h/L and slope, two values where given; the −1.3 area reduction uses the zone area | Fig. 27.3-1 and note | ✓ (the area reduction applies to the area the Cp acts on, so the zone area is right here, unlike F1) |
| MWFRS minimum | 16 psf walls + 8 psf roof on projected areas | 27.1.5 | ✓ |
| Load cases | 1–4, 75% / 56.3%, e = 0.15 B, the exception for cases 1 and 3 | Fig. 27.3-8 | ✓ |

## Smaller notes

- **N1.** Only the MWFRS directional procedure (Chapter 27) is offered. The low-rise envelope procedure
  (Chapter 28) often gives different, frequently lower, shear-wall forces for h ≤ 60 ft. It could be added as
  an option.
- **N2.** The Detailed calculations wall table shows qh for the windward wall. The shear-wall force uses qz up
  the wall, so the table is slightly conservative for the windward wall and only differs in display.
- **N3.** Roof overhang C&C pressures (`windPressureOverhang_External`) are declared but not calculated or
  shown.
- **N4.** The open data items in `PROGRESS.md` still stand: Fig. 30.3-2A Zone 2 −2.3 vs −2.5 overhang, 7-16
  Fig. 30.3-2E Zone 1, 7-16 2H/2I limits, 7-22 hip roofs at 27–45°, MWFRS hip roofs, and 7-16 gable-end walls.

## Sources

The ASCE 7 standards themselves (not available online). Secondary sources used for cross-checks:

- Kd moved from qz to the design-pressure equations in 7-22, and qh = 0.00256 Kh Kzt Ke V²:
  [Carlisle SynTec, Adjustments between ASCE 7-16 and 7-22](https://www.carlislesyntec.com/en/Resources/Media/Blog-Landing-Page/SpecTopics/2025/02/20/Adjustments-Between-ASCE-7-16-and-ASCE-7-22);
  [GAF, Summary of roof-related ASCE 7-22 changes](https://www.gaf.com/en-us/blog/building-science/prevailing-winds-and-prevailing-codes-a-summary-of-roof-related-asce-7-22-changes-281474980198780);
  [Professional Roofing, Updating the standard](https://professionalroofing.net/articles/article/5088)
- The 7-22 Kz profile was adjusted (small reduction in B and C, D unchanged):
  [SEAOG, ASCE 7-22 wind changes](https://seaog.org/images/meeting/031825/seaog_asce_7_22_wind_changes.pdf)
- 7-22 Kz = 2.41(z/zg)^(2/α), Exposure C α = 9.8, zg = 2460 ft; Kz at 30 ft; z_min 30/15/7 ft:
  [steelcalculator.app wind load tool](https://steelcalculator.app/tools/wind-load/);
  [windload.solutions, Exposure D](https://windload.solutions/exposure-d-coastal-ocean-wind-loads);
  [windload.solutions, Exposure B](https://windload.solutions/exposure-b-suburban-residential-wind-loads)
- C&C 16 psf minimum, 30.2.2:
  [Santa Ana calculation package citing 30.2.2](https://publicdocs.santa-ana.org/BuildingWeblink/0/doc/388864/Page93.aspx);
  [Everett WA wall calculation](https://lfportal.everettwa.gov/WebLink/0/doc/1649427/Page16.aspx)
- Effective wind area definition (the fastener tributary limit):
  [Structure Magazine, ASCE 7-22 changes to C&C provisions](https://www.structuremag.org/article/asce-7-22-changes-to-component-and-cladding-wind-provisions/);
  [calcs.com, Wind load analysis to ASCE 7](https://calcs.com/blog/wind-load-analysis-to-asce-7-a-guide-for-structural-engineers)

## Resolution (2026-10-30)

- **F1 fixed.** `WindParameters_Base.RoofComponentArea` / `WallComponentArea` (default 10 sq ft) set the
  effective wind area. Every C&C zone reads its GCp curve at that area. The Building page has a "Cladding
  component" choice for roof and walls: sheathing and fasteners (10 sq ft), framing (span × span/3), or an area
  you enter.
- **F2 fixed.** C&C net pressures are not less than 16 psf in either direction. `PressureData.MinimumGoverns`
  says when the minimum applied, and the Detailed calculations tables note it.
- **F3 closed.** Confirmed by the engineer: Kz is constant from 0–15 ft and follows the power law above that,
  with constants for each exposure. That is what the code does. The "30 ft for Exposure B" in a secondary source
  is the gust-factor z_min, not a Kz floor.
- Tests (`ComponentAreaTests`) include a hand calculation: 7-22 flat roof Zone 3 at 10 sq ft,
  Kh = 0.851, qh = 28.8 psf, p = 28.8 × 0.85 × (−3.2 − 0.18) = −82.8 psf.
