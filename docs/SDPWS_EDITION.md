# Which SDPWS edition are the tables from?

_2026-10-30. Files: `NDSProvisions/Table4_2A-C.cs`, `Table4_3A-C.cs`. Factors: `NDSProvisions/SdpwsSource.cs`.
Tests: `ShearWallCalculator.Tests/SdpwsTests.cs`._

## Conclusion
The tables were typed from **SDPWS 2015**. The 2008 edition used the same layout and, as far as I could check, the
same wood structural panel values, so 2008 can't be fully ruled out from the numbers alone. 2008 is not referenced by
any current IBC. For **wind design the wind column is valid under both 2015 and 2021**, because the 2021 single
nominal value v_n equals the 2015 wind value v_w. The app therefore offers both editions. Only the factors listed
below depend on the edition. The default follows the ASCE 7 edition: 7-22 → IBC 2024 → SDPWS 2021; 7-16 → IBC
2018/2021 → SDPWS 2015.

## Evidence
1. **Layout.** Each row has seismic v_s with G_a (OSB/PLY) and a separate wind v_w, with v_w = 1.4 v_s within
   rounding. A test checks this for every row of Table 4.3A. That is the 2015 (and 2008) format. SDPWS 2021
   replaced the two columns with one nominal value v_n for both wind and seismic, and moved the wind/seismic
   difference into the reduction factors ([Structure magazine, 2021 SDPWS](https://www.structuremag.org/article/2021-special-design-provisions-for-wind-and-seismic/);
   [WoodWorks, P. Line, 2021 SDPWS slides](https://www.woodworks.org/wp-content/uploads/presentation_slides-line-2021-sdpws-211104.pdf)).
2. **Numbers.** These values in our data match published sources:
   - **15/32" sheathing-grade, 8d @ 2" o.c.:** wind 1790 plf (seismic 1280) here. A WoodWorks SDPWS-**2021** worked
     example uses v_n = 1790 plf for the same panel and nailing
     ([WoodWorks, Overlooked code provisions](https://www.woodworks.org/wp-content/uploads/presentation_slides_Cagle_Landry_Overlooked_Code_Provisions_09.2024.pdf)).
     So the 2021 v_n is the 2015 wind value.
   - **Structural I 15/32" 8d:** wind 785 / 1205 / 1540 / 2045 here, and the same values in WoodWorks Shearwalls'
     Table 4.3A listing ([WoodWorks Shearwalls help](https://help.woodworks-software.com/WoodWorks/OnlineHelp/USA/Shearwalls/1266.htm)).
   - **Structural I 5/16" 6d:** wind 560 / 840 / 1090 / 1430 here, and the same values in the same listing.
3. **What changed in 2021 that is not in the data:**
   - Table 4.3A footnote 10: a 0.92 factor with 10d nails and the hold-down on the inside face of the end post.
   - Nail length and diameter columns.
   - The single v_n column.

   ([Structure magazine](https://web-media.awc.org/wp-content/uploads/2021/12/17210645/282107-C-CodesStandards-Line-1.pdf);
   [AWC 2021 SDPWS errata](https://awc.org/wp-content/uploads/2024/05/2021SDPWS-Errata-05.05.24.pdf))

## What the app applies (wind)
| Item | SDPWS 2015 | SDPWS 2021 | In code |
|---|---|---|---|
| Nominal value | wind column v_w | v_n (= v_w) | table wind column |
| ASD | v_w / 2.0 | v_n / 2.0 | `DesignUnitShearWind` |
| LRFD | 0.80 v_w | 0.80 v_n | `DesignUnitShearWind` |
| Aspect ratio over 2:1 (WSP) | × (1.25 − 0.125 h/b) | same | `AspectRatioFactor` |
| Segments of different h/b in one line | 2b/h alternative | same | `CompatibilityFactor` |
| Maximum aspect ratio (blocked WSP) | 3.5:1 | 3.5:1 | `SDPWS_Table4_3_4` |
| 10d nails + hold-down on inside face | — | × 0.92 | `InsideFaceHoldownFactor` |

**Please confirm against your copy:**
- Ω_D = 2.0 and φ_D = 0.80 for wind (2015 §4.3.3, 2021 §4.1.4).
- The 1.25 − 0.125 h/b factor is in 2015 as well as 2021 (Simpson Strong-Tie describes it as new in 2015:
  [SE blog](https://seblog.strongtie.com/tag/deflection-compatibility-criteria/)).

Seismic values are not used: this app designs for wind only. If seismic is ever added, 2021 uses Ω = 2.8 /
φ = 0.50 on v_n, while 2015 uses the seismic column with 2.0 / 0.80.

## Not verified
I could not read the AWC PDFs directly: they are encrypted, and I did not download any PDF tools. The tables are
identified from the layout and from the value matches above, not from a cell-by-cell comparison. A spot check of a
few rows against your printed copy would close this.
