using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CPOLICE
{
    public class MapComponent_PoliceStrobe : MapComponent
    {
        private readonly HashSet<CompPoliceStrobe> active = new HashSet<CompPoliceStrobe>();
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
                        comp.RestoreAfterLoad(pawn);
                    }
                }
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (active.Count == 0)
            {
                return;
            }

            removeBuffer.Clear();
            foreach (CompPoliceStrobe comp in active)
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

        public override void MapRemoved()
        {
            foreach (CompPoliceStrobe comp in active)
            {
                comp?.CleanupFromMapRemoval();
            }

            active.Clear();
            removeBuffer.Clear();
            base.MapRemoved();
        }
    }
}
