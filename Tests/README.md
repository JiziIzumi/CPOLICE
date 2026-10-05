# Validation

XML integration: `python -m unittest discover -s Tests -v` (Python + lxml).
The loader applies the shipped patch subset to explicit CPOLICE definitions; it is not a game/XML-inheritance emulator.

Shield unit harness, compiling the actual production StatPart with minimal dependency stand-ins:

```
mcs -out:/tmp/ShieldMovementHarness.exe Tests/ShieldMovementHarness.cs Source/CPOLICE/StatPart_PoliceShieldMovement.cs
mono /tmp/ShieldMovementHarness.exe
```

Full supported SDK build: `dotnet build Source/CPOLICE/CPOLICE.csproj -c Release`.
The GitHub build workflow also compiles sources and commits the DLL on main.

Shoulder-light lifecycle harness (executes both production components):

```
mcs -langversion:latest -out:/tmp/StrobeLifecycleHarness.exe Tests/StrobeLifecycleHarness.cs Source/CPOLICE/CompPoliceStrobe.cs Source/CPOLICE/MapComponent_PoliceStrobe.cs
mono /tmp/StrobeLifecycleHarness.exe
```

This models direct map transfers, not all caravan/transport transitions or game serialization.
Unspawned wearers retain the existing turn-off behavior.

Optional field reflection audit (requires RimWorld reference assemblies and Rocket's Ranks DLL):

```
mcs -out:/tmp/XmlFieldAudit.exe Tests/XmlFieldAudit.cs
mono /tmp/XmlFieldAudit.exe /path/to/rimworld/ref/net472 /absolute/path/to/CPOLICE /path/to/RocketsRanks.dll
```

It checks `1.6/Defs` fields, excluding custom XML loaders and Def-reference values.
CE external types and full upstream XML inheritance require separate source checks.
