# ASCE 7 Coefficient Data — Verification Report

_2026-10-29 · Scope: W7 `WindLoadCalculations/ASCE7/**` (every `ExternalGCpCurve` definition)_

> **Status 2026-10-29:** V1–V5 have been corrected in the data, and every curve is checked automatically by `Chapter30_CladdingTests.AllCurves_AreSortedSingleSignedAndNonIncreasingInMagnitude`. Open review items are listed in `PROGRESS.md`.

## Method

I used two independent checks.

1. **Internal-consistency check (automated).** ASCE 7 C&C figures plot GCp against **log₁₀(A)**, with straight lines between plateau points. For each curve I found the two plateau ends and checked that every intermediate sample lies on the log-linear line between them. Deviations above ±0.035 are flagged. This catches typos without depending on anyone's memory of the code. The script is described in `ROADMAP.md` §M1 and will become a unit test.
2. **Spot comparison against ASCE 7.** I compared key anchor values with the published figures from memory. These are marked **✅ matches** or **⚠ verify**. Please confirm every ⚠ against your printed copy. I cannot quote the standard directly here.

## Summary

| Area | Result |
|---|---|
| Fig. 30.3-1 walls (both editions) | ✅ Anchors match (Zone 4: +1.0/+0.7 and -1.1/-0.8; Zone 5: +1.0/+0.7 and -1.4/-0.8). One sample is off (below). |
| Fig. 30.3-2A roof θ ≤ 7° | ✅ Zone 1', 1 and 3 anchors are correct. ❌ Zone 2 anchor suspect. ❌ Overhang sign typos. |
| Fig. 30.3-2B to 2I (7-16), 2B to 2G (7-22) | ✅ Log-consistent apart from small sampling drift. ❌ One sign typo in 7-22 2G. |
| Fig. 27.3-1 MWFRS roof Cp (7-16) | ✅ Normal-to-ridge (h/L ≤ 0.25 and ≥ 1.0) and parallel-to-ridge tables match, including the -1.3 area reduction (1.0/0.9/0.8). |
| Fig. 27.3-1 MWFRS walls | ✅ Cp WW = 0.8 and SW = -0.7. ⚠ LW interpolation shortcut (D9). |
| Table 26.13-1 GCpi | ✅ ±0.18 / ±0.55 / ±0.18 / 0.00 |
| Kd | ✅ 0.85. In 7-22 it is correctly moved from qz into the pressure equation. |
| Table 26.10-1 Kz (7-16) | ✅ Constants are correct. ⚠ C&C Exposure B 30 ft rule (D8). |
| Table 26.10-1 Kz (7-22) | ⚠ Uses the 7-16 constants (D7). |

**Overall:** the data you transcribed is accurate to within roughly ±0.03 over almost all of it. The real problems are **five data-entry errors**, listed below. All of them can be fixed in a few lines without changing your data structure.

## Errors found

| ID | File : line | Curve | Current | Expected | Effect |
|---|---|---|---|---|---|
| V1 | `ASCE7_22/CC/Figure30_3_ASCE7_22/Figure30_3_2G_ASCE7_22.cs:22-25` | Roof neg, Zone 3 | `(100, 1.0) … (1000, 1.0)` | `-1.0` (sign) | 🔴 Suction becomes pressure for A ≥ 100 sf |
| V2 | `ASCE7_16/CC/…/Figure30_3_2A_ASCE7_16.cs:117` and `ASCE7_22/…_2A_ASCE7_22.cs:116` | Overhang, Zone 3 | `(200, 1.6)` | `(200, -1.6)` | 🔴 Interpolated overhang GCp swings positive between 100 and 500 sf |
| V3 | same files, l.128/127, 139/138, 150/149 | Overhang, Zones 2, 1, 1' | `(200, 1.36)`, `(200, 1.32)` ×2 | negative | 🔴 Same effect |
| V4 | same files, l.77/76 (roof) and l.124/123 (overhang) | Zone 2 at A ≤ 10 sf | `-2.5` | ⚠ **likely -2.3** | 🟠 Your own samples at 20, 50, 100 and 200 sf (-2.15, -1.92, -1.78, -1.63) lie exactly on a -2.3 → -1.4 line. Only the plateau value is off. |
| V5 | `Figure30_3_1_ASCE7_16.cs:21,33` and `…_22.cs:22,34` | Wall pos, Zones 4 and 5 at 50 sf | `0.83` | `≈0.88` | 🟡 About 5% low at that point |

## Overhang curves need a closer look

The overhang sets in `Figure30_3_2A_*` (Zones 1 and 1') read `-1.7, -1.65, -1.62, -1.60, (+)1.32, -1.0`. That is not a log-linear shape. It looks partly hand-edited, or copied from another zone. Please recheck these two curves against the figure as a whole, not just the sign.


## Roof overhangs (item 3b): rules used, and items to check

- **MWFRS, §27.3.3 (ASCE 7-22 text, confirmed by the user):** "the positive external pressures on the bottom surface of windward roof overhangs shall be determined using Cp = 0.8 and combined with the top surface pressures determined using Figure 27.3-1."
  - Implemented as soffit = qh·G·0.8·Kd, with no internal pressure (the soffit is outside the building).
  - The top surface uses the windward roof at the eave: the windward roof plane (wind normal to the ridge), otherwise the 0 to h/2 distance zone. The most negative Cp is used.
  - This is roof uplift only. It does not change the shear-wall forces.
- **C&C, ASCE 7-16:** the Fig. 30.3-2A–I overhang curves include both surfaces, so no internal pressure is applied.
- **C&C, ASCE 7-22:** the overhang graphs were removed for gable roofs over 7° and for all hip roofs (STRUCTURE magazine, Oct 2022, "ASCE 7-22 changes to C&C provisions"). Overhang GCp = roof-zone GCp + positive wall GCp.
  - Example from that source, Zone 2: −2.0 + 1.0 = −3.0.
  - **To verify:** the article doesn't say which wall zone or tributary area applies to the wall term. The app uses Zone 5 under the corner roof zones (3, 3e, 3r) and Zone 4 elsewhere, read at the roof component's area. Please check this against the printed Chapter 30 text and correct it if needed.
- **To verify:** in the 7-22 Fig. 30.3-2A data, overhang Zone 3 reads −3.2 at 10 sq ft, the same as roof Zone 3. Confirm against the printed figure.

## Minor drift (no action strictly needed)

These are 0.04–0.06 deviations at single interior samples:

- 7-16 2B, Zones 3e/2n/2r at 200 sf
- 7-16 2C, Zone 3r at 10 and 20 sf
- 7-16 2D, Zones 3r/2n at 50 sf
- 7-16 2G, positive curves at 50 sf
- 7-16 2H/2I, positive curves at 10 sf

Most are probably reading error from the chart. **Recommendation:** store each curve as its **anchor points only** (the plateau ends, exactly as printed) and interpolate on log₁₀(A) in code. That removes this whole class of error and makes the data match the standard line for line.

## Logic items that affect the numbers

See `AUDIT.md` for details:

- **D5** — h/L = 0.5 returns no curve
- **D6** — gable-end height
- **D7/D8** — Kz
- **D9** — leeward Cp
- **D10** — G and I inputs
- **(60, 0.01·θ)** in `NormToRidge_*` windward-positive: for 45° < θ < 60° this interpolates toward 0.01·θ, not toward 0.6. That is a small error, but encoding the table as "θ ≥ 60° ⇒ 0.01θ" would be cleaner.

## Chapter 28 envelope procedure (W7 bc95ffd) — to verify against the printed Fig. 28.3-1

1. **Confirmed by the user (Case A and the 5/6 values). Case B zones 1 and 4 are the parallel side walls, equal in area: they act across the wind and cancel, so they do not enter the shear-wall force.** **GC_pf table, Case A** (θ: 1, 2, 3, 4, 1E, 2E, 3E, 4E): 0–5°: 0.40, −0.69, −0.37, −0.29, 0.61, −1.07, −0.53, −0.43; 20°: 0.53, −0.69, −0.48, −0.43, 0.80, −1.07, −0.69, −0.64; 30–45°: 0.56, 0.21, −0.43, −0.37, 0.69, 0.27, −0.53, −0.48; 90°: 0.56, 0.56, −0.37, −0.37, 0.69, 0.69, −0.48, −0.48. **Case B:** 5 = 0.40, 6 = −0.29, 5E = 0.61, 6E = −0.43. Same in 7-16 and 7-22?
2. **Confirmed by the user.** **Torsional load cases:** the T zones taken as everything beyond the building center on the side away from the reference corner, at 25%. Confirm the T-zone extent in the figure.
3. **Removed (W7 f70f235): Zone 2 and Zone 3 run the full slope from eave to ridge in both editions (user); there is no 2.5 he limit.** Formerly: negative Zone 2/2E applied up to min(0.5 × building dimension parallel to the wind, 2.5 × eave height); the rest to the ridge uses Zone 3/3E.
4. **Confirmed (28.3.2.1):** torsional cases not required for one-story buildings with h ≤ 30 ft, one- and two-story light-frame buildings, or one- and two-story buildings with flexible diaphragms.
5. **Hip roofs:** user confirms Figs. 28.3-1 and 28.3-2 (7-22) do not mention hip roofs, so Chapter 28 falls back to Chapter 27 for them.
6. **Torsional zone extents (user, 7-22 Fig. 28.3-2):** Zone 2 over L − 2a (Case 1), 0.5L − 2a (Case 3). Case 4 (along the ridge): 5E/6E are a wide and 5/6 run 0.5B − a (user, from the figure); applied for 7-22 and, per the user, 7-16 (W7 4bea57d). Case A torsional: 2E 2a from the corner, 2 the remainder to the center, 2T the back half (same for 3) — confirmed, matches the app. Case 2 (basic, along the ridge): 5E/6E a wide, 1E/3E 2a wide (user); applied (W7 f70f235).
7. **Zones on the walls (user, 2026-10-30).** Wind normal to the ridge (7-16 Case A; 7-22 Load Cases 1 and 3): no zones on the gable end walls; 1/1T/1E and 4/4T/4E on the walls parallel to the ridge. Wind along the ridge (7-16 Case B; 7-22 Load Cases 2 and 4): zones on all four walls. Matches W7. Every wall area's force is now computed and summed with its sign (W7 47bd5de), so the side-wall zones that cancel in Vx still show in the tally and enter the torsion.
8. **Case B values confirmed by the user (7-16):** 2 = −0.69, 3 = −0.37, 1E = 4E = −0.48, 2E = −1.07, 3E = −0.53 (1 = 4 = −0.45 as before).
9. **Still assumed (not answered):** in the Case B torsional case only 5T/6T take the 25% reduction; the side walls and the roof keep full pressure.

## SDPWS sheathing Tables 4.3B and 4.3C, and the two-face rules (SWC) — to verify against the printed SDPWS

1. **Table 4.3C rows 1 and 2 are the same row** in the typed table (1/2" gypsum wallboard, 7" edge / 24" studs, unblocked). The app lists it once.
2. **Table 4.3B rows 10–11 are plywood siding** but are typed as `SHEATHING_AND_SINGLE_FLOOR`; the app names them plywood siding from their note. Row 11's note says "8d (3.0 × 0.128)", which is the size of a 10d common nail. Check both rows.
3. **Table 4.3.4 aspect limit for unblocked gypsum wallboard:** the app uses 2:1 for every Table 4.3C row (1:1 is used by some editions for unblocked gypsum). Check against the edition in use.
4. **Structural fiberboard aspect factor** 1.09 − 0.09 h/b above 1:1 (up to 3.5:1): check against the printed 4.3.4.2.
5. **Gypsum lath rows 24 and 25** (360 vs 200 plf): the difference between them looks large; check the values.
6. **Two faces (user, 2026-10-31):** same material on both faces = 2 × one face; different materials = the larger of the stronger face and 2 × the weaker (SDPWS 2021 4.3.5.4.2); for wind only, a wood structural panel or structural fiberboard face plus a gypsum wallboard face are added (4.3.5.4.2 exception) — a project setting, on by default. Gypsum + cement plaster, or wood + plaster, do not add. A face over its h/b limit does not count.
