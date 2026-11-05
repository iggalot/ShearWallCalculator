# Roadmap / Parking Lot

Ideas and unfinished work across the suite (BuildingModel, Wind Load Calculator, Load Manager,
Shear Wall Calculator, WoodFraming, NDS_WoodLibrary). Pick items off one at a time; move them to
**Done** with the date and commit when finished.

Status tags: `[ ]` not started, `[~]` in progress, `[x]` done.

## Next up

- [ ] **Central hub (launcher) for the apps.** One window that opens the project (.bldg.json /
  project folder) and launches each app on it: Building Model editor, Wind Load Calculator,
  Load Manager, Shear Wall Calculator, and later apps.
  - Each app can be enabled or disabled by access level or authorization (a per-user or per-license
    list of the apps they may run; a disabled app shows greyed out with the reason on hover).
  - For now everything is authorized. Keep the authorization check in one place so it can later
    read a license or a user file.
  - Shows the project folder, the model's status, and which outputs are out of date.

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
