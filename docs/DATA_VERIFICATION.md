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
