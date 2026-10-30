# Wind Engine Compatibility

ShearWallCalculator builds against the sibling repository `..\ASCE7-Wind-Load-Calculator` (W7) through
project references:

- `ASCE7Wind.Core` — the WPF-free engine
- `ASCE7WindLoadCalculator` — the WPF input and results controls

Check out a matching pair of commits when you build an older version.

| ShearWallCalculator branch / commit | W7 branch / commit | Notes |
|---|---|---|
| `CC_windcalcs @ 0c64be5` | `main @ c67831c` (+ uncommitted edit) | Pre-audit baseline |
| `dev/wind-core-refactor @ 0359acb` | `dev/wind-core-refactor @ 14b4bb2` | Core extracted, editions as data, code-checked tests, defect fixes |
| `dev/wind-core-refactor @ ed67539` (M3/M4) | `dev/wind-core-refactor @ 801e93b` | 3D building model, wind scene, 3D viewer tab and window |
| `dev/wind-core-refactor` (fixes after first test drive) | `dev/wind-core-refactor @ 2e54420` | Crash fixes, ridge-correct zoning, tapered windward wall, pressure blocks, layout |
| `dev/wind-core-refactor @ ea3703e` (M6) | `dev/wind-core-refactor @ 17d5075` | Lateral wind loads, Fig. 27.3-8 load cases into the shear walls |
| `dev/wind-core-refactor @ ef727a9` (M5) | `dev/wind-core-refactor @ 2f7bcf4` | Guided Wind Setup tab (Site, Building, Results) |
| `dev/wind-core-refactor @ 733a36c` (M5 revisions) | `dev/wind-core-refactor @ c0fb7b2` | One-screen steps, prototype results layout, live shear-wall link |
| `dev/wind-core-refactor` (M5 polish) | `dev/wind-core-refactor @ e122a8c` | Wind workspace, default wind link, compact viewer |
| `dev/wind-core-refactor` (third look) | `dev/wind-core-refactor @ 0f35b75` | Workspace switch, stepped site page, detailed calculations window, LOADS dialog |
| `dev/wind-core-refactor` (fourth look) | `dev/wind-core-refactor @ daa404c` | Edition as Step 1, tinted cards; shear-wall guidance and NaN fix; equation audit |

## Building

- Visual Studio 2026 (MSBuild 18), .NET Framework 4.7.2 targeting pack.
- Test projects restore MSTest **only from the local NuGet cache** (`NuGet.config` in each test folder),
  so nothing is downloaded.

## Running the tests

From a shell, run:

```
MSBuild ShearWallCalculator.sln -restore
vstest.console ShearWallCalculator.Tests\bin\Debug\net472\ShearWallCalculator.Tests.dll
vstest.console ..\ASCE7-Wind-Load-Calculator\ASCE7Wind.Tests\bin\Debug\net472\ASCE7Wind.Tests.dll
```

The 3D viewer's rendering tests live in the wind solution. Build `ASCE7WindLoadCalculator.sln`, then run:

```
vstest.console ASCE7Wind.UI.Tests\bin\Debug\net472\ASCE7Wind.UI.Tests.dll
```

Set `SNAPSHOT_DIR` to a folder to also save PNG renders of every view.

You can also use Test Explorer in Visual Studio.
