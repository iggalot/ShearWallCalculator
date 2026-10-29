# Wind Engine Compatibility

ShearWallCalculator builds against the sibling repository `..\ASCE7-Wind-Load-Calculator` (W7) through
project references:

- `ASCE7Wind.Core` — the WPF-free engine
- `ASCE7WindLoadCalculator` — the WPF input and results controls

Check out a matching pair of commits when you build an older version.

| ShearWallCalculator branch / commit | W7 branch / commit | Notes |
|---|---|---|
| `CC_windcalcs @ 0c64be5` | `main @ c67831c` (+ uncommitted edit) | Pre-audit baseline |
| `dev/wind-core-refactor` (this branch) | `dev/wind-core-refactor @ 14b4bb2` | Core extracted, editions as data, code-checked tests, defect fixes |

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

You can also use Test Explorer in Visual Studio.
