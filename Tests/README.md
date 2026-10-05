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
