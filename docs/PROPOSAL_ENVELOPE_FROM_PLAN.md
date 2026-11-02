# Proposal: take the wind building from the shear-wall plan

_2026-11-01. Branch `dev/wind-core-refactor` in both repos. For you to decide on; nothing here is built yet._

## The problem

The same building is described twice:

- **Wind Setup** asks for a crude box: L, B, eave height (now the sum of the story plate heights), floors, roof.
- **The shear-wall plan** already holds the exact building: each level's diaphragm (the floor or roof outline), its walls and their heights, and the number of levels.

The user re-types what the plan already says, and the two can disagree without anything saying so. For example, the plan is 64 ft long but the wind was run on a 60 ft box. Or level 2 is set back, but the wind is applied as if it were full width.

## The idea in one line

**The plan becomes the source of the wind building's geometry.** The Shear Wall Designer works out the envelope from its levels and hands it to the wind engine. The Wind Load Calculator stays a separate app that takes the same inputs as today, typed or supplied.

## Keeping the two apps separate

```
 Shear Wall Designer                        shared library            Wind Load Calculator (standalone)
 ─────────────────────                      ASCE7Wind.Core            ─────────────────────────────────
 levels (walls, diaphragms,  ──► Envelope ──► BuildingData  ◄── typed in its own Building step
 plate height per level)        extractor     + per-story widths (new, optional)
                                              WindAnalysis / StoryWindLoads
 per-level load cases        ◄────────────── StoryWindResult
```

- **The contract is the data object that already exists, `BuildingData`.** It carries L, B, plate heights, roof, overhang, parapet and enclosure.
- **One optional addition:** each story's width normal to the wind, in each direction. When it is missing, the story uses the building's B or L, which is exactly today's result.
- **The wind app never references shear-wall code.**
  - The Shear Wall Designer only builds a `BuildingData` and calls the engine.
  - Standalone, the wind app keeps its own Building step, unchanged.

## What comes from the plan, and what is still typed

| Input | From | How |
|---|---|---|
| Length L (W-E), width B (S-N) | **plan** | Extents of all levels' diaphragms (the outside faces of the exterior walls). |
| Number of floors | **plan** | The levels (ADD FLOOR / DELETE FLOOR). |
| Story plate heights, so the eave and the floor elevations | **plan** | One plate height per level, set when the floor is added. Walls on the level default to it. |
| Each story's width normal to the wind | **plan** | That level's diaphragm extents in that direction (catches set-backs). |
| Roof type, pitch, ridge, overhang, parapet | typed | The roof is not drawn. Kept in Wind Setup. |
| Site (V, exposure, Kzt, risk) and enclosure | typed | Not geometry. |

Inside the Shear Wall Designer, the plan-driven fields in Wind Setup:
- show as read-only, marked "from the plan", next to a **Type them instead** switch;
- that switch is for a quick early run before anything is drawn, or for an envelope the plan does not show.

While the switch is on, any difference from the plan larger than 0.5 ft (or 2 %) is flagged on Wind Setup and on the Calculations tab.

## Making the method correct for multistory

1. **The coefficients use the whole building.**
   - h, L/B, h/L and the zone width a come from the overall envelope: the extents of all levels, the total of the plate heights, then the roof.
   - Chapter 27 and Chapter 28 define them for the building, not per story.
2. **The forces use each story's own wall.**
   - Today each diaphragm takes wall pressure × B × strip height, from mid-story to mid-story.
   - With per-story widths, a set-back upper story takes only its own width.
   - The floor below takes its own width for its strip, plus the lower roof beside the set-back.
   - The floors still add up to the building's base shear for a plain box (tested), and set-backs give less than the box.
3. **Where the load acts.**
   - Today each level's case goes in at its center of mass, with the 0.15 B eccentricity of the building.
   - With the plan, it goes in at the middle of that level's projected width, with 0.15 × that level's width (Fig. 27.3-8 per level).
   - This changes the torsion for set-back or off-center upper stories, and it is the more correct load path.
4. **What the box cannot describe gets flagged, not hidden.**
   - Examples: an L-shaped plan, or a level less than about 75 % of the one below.
   - The app says the wind used the enclosing rectangle and the per-story widths, and that ASCE 7's rectangular-building procedures are being applied by judgment.
   - The calculation stays conservative in those cases: the enclosing rectangle for the coefficients, and the true projected width for the forces.
5. **One drawing convention to settle.**
   - The diaphragm is drawn to the **outside face** of the exterior walls, so its extents are the building's.
   - The sample DXF follows this: outside faces 60 × 40 ft.
   - A wall on the diaphragm edge is an exterior wall.

## Phases (each one compiles and has unit tests before the next starts)

1. **Envelope extractor (Shear Wall Designer core).** `BuildingEnvelope.From(levels, roof inputs)` returns:
   - a `BuildingData`;
   - the per-story widths;
   - the warnings (non-rectangular plan, set-backs, a level with no diaphragm, walls whose height differs from the level's plate height).

   Tests: a box, a set-back third floor, an L-shape, an empty level.
2. **Per-story widths in the wind engine (ASCE7Wind.Core).**
   - `StoryWindLoads.Compute` takes optional widths.
   - Test: equal widths reproduce today's numbers exactly; a set-back reduces only its own strip.
3. **Plan-driven Wind Setup inside the Shear Wall Designer.**
   - The fields are locked, with the switch to type them instead and the mismatch warnings.
   - The plate height moves onto the level as the source of truth; Wind Setup shows it.
   - It updates live as walls or diaphragms change, debounced.
4. **Loads at each level's own projected width and eccentricity** (item 3 of the method). This is followed by a hand-checked two-story set-back example in the tests.
5. **A 3D sketch of the building from the plan.**
   - Each level's walls are drawn standing at their elevations, with the diaphragms as slabs. The roof comes from the roof inputs.
   - The wind envelope box (and the windward / leeward markers) is drawn over it, so you can see the wind building is the drawn building.
   - It goes in its own tab in the Shear Wall Designer, built on WPF 3D the same way as the wind app's view, with no packages.

The standalone wind app only gets phase 2's optional widths. Its Floors and plate-height boxes, added today, stay for standalone use.

## Questions for you

1. **The diaphragm drawn to the outside face of the exterior walls.** Is that how you draw it, or do you draw it to the wall centerlines? With centerlines, L and B would add the wall thickness.
2. **Non-rectangular plans.** Is the enclosing rectangle for the coefficients, with the true projected widths for the forces, acceptable? The other option is to refuse anything that is not a rectangle.
3. **The "Type them instead" switch.** Should the plan-driven fields always follow the plan (no switch)?
