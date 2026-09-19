using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CPOLICE
{
    public class CompProperties_PoliceStrobe : CompProperties
    {
        public float whiteRadius = 5.5f;
        public float strobeRadius = 4f;
        public int strobeIntervalTicks = 10;

        public CompProperties_PoliceStrobe()
        {
            compClass = typeof(CompPoliceStrobe);
        }
    }

    public class CompPoliceStrobe : ThingComp
    {
        private enum LightMode : byte
        {
            Off,
            White,
            Strobe
        }

        private LightMode mode;
        private int strobePhase;
        private int nextStrobeTick;

        private CompGlower glower;
        private Map glowerMap;
        private IntVec3 glowerPosition = IntVec3.Invalid;
        private int glowerSignature;

        private CompProperties_PoliceStrobe Props => (CompProperties_PoliceStrobe)props;
        private Apparel Apparel => parent as Apparel;
        private Pawn Wearer => Apparel?.Wearer;

        internal bool IsActive => mode != LightMode.Off;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref mode, "cpoliceLightMode", LightMode.Off);
            Scribe_Values.Look(ref strobePhase, "cpoliceStrobePhase", 0);
            Scribe_Values.Look(ref nextStrobeTick, "cpoliceNextStrobeTick", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                strobePhase = ((strobePhase % 4) + 4) % 4;
                nextStrobeTick = 0;
            }
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetWornGizmosExtra())
            {
                yield return gizmo;
            }

            Pawn pawn = Wearer;
            if (pawn == null || pawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "肩灯：" + ModeLabel(),
                defaultDesc = "切换肩灯模式：关闭 → 白色常亮 → 红蓝爆闪。白灯会实际照亮周围区域；红蓝爆闪按红 → 灭 → 蓝 → 灭循环，仅提供照明与视觉警示。",
                icon = parent.def.uiIcon,
                action = CycleMode
            };
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (mode != LightMode.Off)
            {
                RegisterWithMap(pawn);
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            TurnOffAndCleanup();
            base.Notify_Unequipped(pawn);
        }

        public override void Notify_WearerDied()
        {
            TurnOffAndCleanup();
            base.Notify_WearerDied();
        }

        public override void Notify_Downed()
        {
            TurnOffAndCleanup();
            base.Notify_Downed();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            RemoveGlower();
            UnregisterFromMap();
            base.PostDeSpawn(map, mode);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            RemoveGlower();
            UnregisterFromMap();
            base.PostDestroy(mode, previousMap);
        }

        internal void RestoreAfterLoad(Pawn pawn)
        {
            if (mode != LightMode.Off && pawn != null && pawn.Spawned && !pawn.Dead && !pawn.Downed)
            {
                RegisterWithMap(pawn);
                TickFromMap();
            }
            else
            {
                RemoveGlower();
            }
        }

        internal bool TickFromMap()
        {
            Pawn pawn = Wearer;
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed || mode == LightMode.Off)
            {
                RemoveGlower();
                return false;
            }

            if (mode == LightMode.White)
            {
                EnsureGlower(pawn, new ColorInt(255, 244, 220), Props.whiteRadius, 1);
                return true;
            }

            int ticks = Find.TickManager.TicksGame;
            if (nextStrobeTick <= 0)
            {
                strobePhase = 0;
                nextStrobeTick = ticks + StrobeIntervalTicks;
            }
            else if (ticks >= nextStrobeTick)
            {
                strobePhase = (strobePhase + 1) % 4;
                nextStrobeTick = ticks + StrobeIntervalTicks;
            }

            switch (strobePhase)
            {
                case 0:
                    EnsureGlower(pawn, new ColorInt(255, 20, 20), Props.strobeRadius, 2);
                    break;
                case 2:
                    EnsureGlower(pawn, new ColorInt(25, 80, 255), Props.strobeRadius, 3);
                    break;
                default:
                    RemoveGlower();
                    break;
            }

            return true;
        }

        internal void CleanupFromMapRemoval()
        {
            RemoveGlower();
            glowerMap = null;
        }

        private int StrobeIntervalTicks => Props.strobeIntervalTicks < 9 ? 9 : Props.strobeIntervalTicks > 15 ? 15 : Props.strobeIntervalTicks;

        private string ModeLabel()
        {
            switch (mode)
            {
                case LightMode.White:
                    return "白色常亮";
                case LightMode.Strobe:
                    return "红蓝爆闪";
                default:
                    return "关闭";
            }
        }

        private void CycleMode()
        {
            mode = mode == LightMode.Off
                ? LightMode.White
                : mode == LightMode.White
                    ? LightMode.Strobe
                    : LightMode.Off;

            RemoveGlower();

            if (mode == LightMode.Strobe)
            {
                strobePhase = 0;
                nextStrobeTick = Find.TickManager.TicksGame + StrobeIntervalTicks;
            }
            else
            {
                nextStrobeTick = 0;
            }

            Pawn pawn = Wearer;
            if (mode == LightMode.Off || pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed)
            {
                UnregisterFromMap();
                return;
            }

            RegisterWithMap(pawn);
            TickFromMap();
        }

        private void TurnOffAndCleanup()
        {
            mode = LightMode.Off;
            strobePhase = 0;
            nextStrobeTick = 0;
            RemoveGlower();
            UnregisterFromMap();
        }

        private void RegisterWithMap(Pawn pawn)
        {
            pawn?.Map?.GetComponent<MapComponent_PoliceStrobe>()?.Register(this);
        }

        private void UnregisterFromMap()
        {
            Map map = Wearer?.Map ?? glowerMap;
            map?.GetComponent<MapComponent_PoliceStrobe>()?.Unregister(this);
        }

        private void EnsureGlower(Pawn pawn, ColorInt color, float radius, int signature)
        {
            if (pawn.Map == null)
            {
                RemoveGlower();
                return;
            }

            if (glower != null && glowerMap == pawn.Map && glowerSignature == signature)
            {
                if (glowerPosition != pawn.Position)
                {
                    glowerMap.glowGrid.DeRegisterGlower(glower);
                    if (glowerPosition.IsValid && glowerPosition.InBounds(glowerMap))
                    {
                        glowerMap.mapDrawer.MapMeshDirty(glowerPosition, MapMeshFlagDefOf.Things);
                    }

                    glowerPosition = pawn.Position;
                    glowerMap.glowGrid.RegisterGlower(glower);
                    glowerMap.mapDrawer.MapMeshDirty(glowerPosition, MapMeshFlagDefOf.Things);
                }
                return;
            }

            RemoveGlower();

            var glowerProps = new CompProperties_Glower
            {
                glowRadius = radius,
                overlightRadius = radius * 0.35f,
                glowColor = color
            };

            glower = new CompGlower
            {
                parent = pawn
            };
            glower.Initialize(glowerProps);

            glowerMap = pawn.Map;
            glowerPosition = pawn.Position;
            glowerSignature = signature;

            glowerMap.glowGrid.RegisterGlower(glower);
            glowerMap.mapDrawer.MapMeshDirty(glowerPosition, MapMeshFlagDefOf.Things);
        }

        private void RemoveGlower()
        {
            if (glower != null && glowerMap != null)
            {
                glowerMap.glowGrid.DeRegisterGlower(glower);
                if (glowerPosition.IsValid && glowerPosition.InBounds(glowerMap))
                {
                    glowerMap.mapDrawer.MapMeshDirty(glowerPosition, MapMeshFlagDefOf.Things);
                }
            }

            glower = null;
            glowerMap = null;
            glowerPosition = IntVec3.Invalid;
            glowerSignature = 0;
        }
    }
}
