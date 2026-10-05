# CPOLICE CE compatibility fixes — 2026-10-05

Starting commit: 11391fd693606989922b00c2dd52796f4d8bc49b.

- Both QSZ92 pistols now carry CE_OneHandedWeapon and CE_Sidearm tags.
- Both belts use CE CarryWeight +5 kg / CarryBulk +8 instead of vanilla CarryingCapacity when CE is active. Vanilla +50 is preserved.
- Armed belt, HK coat, beret and new patrol cap use the existing patch's sharp ×20 / blunt ×40 conversion convention. This preserves the repository's enhanced equipment balance.
- New hats and all rank insignia have explicit Bulk/WornBulk.
- All 13 gun melee tools use ToolCE with blunt penetration 2, retaining their existing power/cooldown.
- CE riot shield uses CE's modern drafted render node and no ordinary worn graphic.
- Shield movement is a ×0.9 pawn-stat transformation, with no additional flat offset, in vanilla and CE.
- CE appears in loadAfter as an optional mod, not a required dependency.

The senior cap's high armor is retained: its original description explicitly specifies a powerful ballistic item. This was a balance observation in the audit, not a confirmed code bug.
No evidence supported deleting source files or changing ammunition identities. All defNames and packageId are preserved.

Validation:
- Python unittest XML integration suite: 10 passing tests; eight initially failed against the original definitions.
- Actual StatPart executed with minimal dependency stand-ins: three movement speeds, removing shield, explanations, empty/non-pawn/no-apparel requests. A deliberately incorrect flat-penalty variant fails.
- Full CPOLICE DLL compiled successfully with Mono against locally available Krafs.Rimworld.Ref 1.6.4633, plus Unity Core/netstandard references.
- Project remains pinned to Krafs.Rimworld.Ref 1.6.4871 for GitHub's normal dotnet build. Downloading that package in this environment timed out; exact pinned-package build is not verified locally.
- XML parsing / explicit patch target resolution / all 13 gun and five ammo-set links checked.
- git diff --check clean.

Limitations: static test loader does not resolve external XML inheritance, CE auto-patching or the full game. No live RimWorld/CE save was launched.

In-game regression checklist:
1. Load Rocket's Ranks, CE, CPOLICE; inspect startup log.
2. Equip shield with each QSZ92 pistol and baton; shoot/attack. Two-handed rifles remain restricted by CE shield logic.
3. Inspect shield on four rotations, drafted/undrafted, standing/crouching.
4. Wear/remove each belt; verify CE max weight/bulk changes by 5/8.
5. Wear/remove shield at differing pawn speeds; verify 10 percent stat penalty.
6. Craft, haul, pick up, drop and reload each ammo type, especially 5.8 mm, in new and old saves.
7. Shoulder light off/white/strobe, save/load, movement, downing and map departure.
