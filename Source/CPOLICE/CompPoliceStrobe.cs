using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CPOLICE
{
    public class CompProperties_PoliceStrobe : CompProperties
    {
        public float whiteRadius = 5.5f;
        public float strobeRadius = 4f;
        public CompProperties_PoliceStrobe() { compClass = typeof(CompPoliceStrobe); }
    }

    public class CompPoliceStrobe : ThingComp
    {
        private enum LightMode : byte { Off, White, Strobe }
        private LightMode mode;
        private Mote lightMote;
        private int nextStrobeTick;
        private bool redPhase;

        private CompProperties_PoliceStrobe Props => (CompProperties_PoliceStrobe)props;
        private Apparel Apparel => parent as Apparel;
        private Pawn Wearer => Apparel?.Wearer;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref mode, "cpoliceLightMode", LightMode.Off);
            Scribe_Values.Look(ref nextStrobeTick, "cpoliceNextStrobeTick", 0);
            Scribe_Values.Look(ref redPhase, "cpoliceRedPhase", false);
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = Wearer;
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed || mode == LightMode.Off)
            {
                DestroyLight();
                return;
            }

            if (mode == LightMode.Strobe && Find.TickManager.TicksGame >= nextStrobeTick)
            {
                redPhase = !redPhase;
                nextStrobeTick = Find.TickManager.TicksGame + 12; // 0.2 s at 60 ticks/s
                DestroyLight();
            }

            if (lightMote == null || lightMote.Destroyed)
                SpawnLight(pawn);
            else
                lightMote.exactPosition = pawn.DrawPos;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
            Pawn pawn = Wearer;
            if (pawn == null || pawn.Faction != Faction.OfPlayer) yield break;

            yield return new Command_Action
            {
                defaultLabel = "肩灯：" + ModeLabel(),
                defaultDesc = "切换肩灯模式：关闭 → 白色常亮 → 红蓝爆闪。爆闪仅为视觉效果。",
                icon = parent.def.uiIcon,
                action = CycleMode
            };
        }

        private string ModeLabel()
        {
            if (mode == LightMode.White) return "白灯";
            if (mode == LightMode.Strobe) return "红蓝爆闪";
            return "关闭";
        }

        private void CycleMode()
        {
            mode = mode == LightMode.Off ? LightMode.White : mode == LightMode.White ? LightMode.Strobe : LightMode.Off;
            redPhase = true;
            nextStrobeTick = Find.TickManager.TicksGame + 12;
            DestroyLight();
        }

        private void SpawnLight(Pawn pawn)
        {
            Color color = mode == LightMode.White ? Color.white : (redPhase ? Color.red : Color.blue);
            float scale = mode == LightMode.White ? Props.whiteRadius : Props.strobeRadius;
            lightMote = MoteMaker.MakeStaticMote(pawn.DrawPos, pawn.Map, ThingDefOf.Mote_PowerBeam, scale);
            if (lightMote != null)
            {
                lightMote.instanceColor = color;
                lightMote.exactPosition = pawn.DrawPos;
            }
        }

        private void DestroyLight()
        {
            if (lightMote != null && !lightMote.Destroyed) lightMote.Destroy();
            lightMote = null;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            DestroyLight();
            base.PostDestroy(mode, previousMap);
        }
    }
}
