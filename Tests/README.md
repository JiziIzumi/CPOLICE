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

This models direct map transfers and a despawn/return transition, not every caravan/transport path or game serialization.
While a wearer is off-map, the glower is removed but its selected mode is retained and restored after the pawn spawns on a map.
