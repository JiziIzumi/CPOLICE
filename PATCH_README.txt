CPOLICE incremental patch

Copy the contents of this archive into the CPOLICE mod root and overwrite matching files.

Implemented:
- Phase 1 visible equipment naming and real-world rank labels. Internal defNames unchanged.
- 8 phase-2 equipment defs with CE Bulk/armor compatibility patch.
- Placeholder functional textures for the new equipment, intended to be replaced by final artwork later.

Important limitation:
- The radio + shoulder light item and +10% work-speed effect are included. The OFF/WHITE/STROBE runtime light controller is NOT enabled in this patch because this environment does not contain RimWorld/Verse assemblies or a C# compiler to build and validate the required DLL safely. I did not ship an untested binary. Source folder only records the intended extension point.
