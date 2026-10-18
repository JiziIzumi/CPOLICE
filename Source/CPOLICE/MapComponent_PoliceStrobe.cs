using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CPOLICE
{
    public class MapComponent_PoliceStrobe : MapComponent
    {
        private const int ActiveScanIntervalTicks = 250;
        private readonly HashSet<CompPoliceStrobe> active = new HashSet<CompPoliceStrobe>();
        private readonly List<CompPoliceStrobe> tickBuffer = new List<CompPoliceStrobe>();
        private readonly List<CompPoliceStrobe> removeBuffer = new List<CompPoliceStrobe>();

        public MapComponent_PoliceStrobe(Map map) : base(map)
        {
        }

        public void Register(CompPoliceStrobe comp)
        {
            if (comp != null)
            {
                active.Add(comp);
            }
        }

        public void Unregister(CompPoliceStrobe comp)
        {
            if (comp != null)
            {
                active.Remove(comp);
            }
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                List<Apparel> worn = pawn.apparel?.WornApparel;
                if (worn == null)
                {
                    continue;
                }

                for (int i = 0; i < worn.Count; i++)
                {
                    CompPoliceStrobe comp = worn[i].GetComp<CompPoliceStrobe>();
                    if (comp != null && comp.IsActive)
                    {
                        active.Add(comp);
                        comp.RestoreAfterMapEntry(pawn);
                    }
                }
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (active.Count > 0)
            {
                removeBuffer.Clear();
                tickBuffer.Clear();
                tickBuffer.AddRange(active);
                foreach (CompPoliceStrobe comp in tickBuffer)
                {
                    if (comp == null || !comp.TickFromMap())
                    {
                        removeBuffer.Add(comp);
                    }
                }

                for (int i = 0; i < removeBuffer.Count; i++)
                {
                    active.Remove(removeBuffer[i]);
                }
            }

            if (Find.TickManager.TicksGame % ActiveScanIntervalTicks == 0)
            {
                RestoreActiveWornLights();
            }
        }

        private void RestoreActiveWornLights()
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                List<Apparel> worn = pawns[i].apparel?.WornApparel;
                if (worn == null)
                {
                    continue;
                }

                for (int j = 0; j < worn.Count; j++)
                {
                    CompPoliceStrobe comp = worn[j].GetComp<CompPoliceStrobe>();
                    if (comp != null && comp.IsActive && !active.Contains(comp))
                    {
                        comp.RestoreAfterMapEntry(pawns[i]);
                    }
                }
            }
        }

        public override void MapRemoved()
        {
            tickBuffer.Clear();
            tickBuffer.AddRange(active);
            foreach (CompPoliceStrobe comp in tickBuffer)
            {
                comp?.CleanupFromMapRemoval(map);
            }

            active.Clear();
            tickBuffer.Clear();
            removeBuffer.Clear();
            base.MapRemoved();
        }
    }
}
