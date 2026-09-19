using RimWorld;
using Verse;

namespace CPOLICE
{
    public class ThoughtWorker_CPOLICE_ApparelMood : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p?.apparel == null)
            {
                return ThoughtState.Inactive;
            }

            string requiredApparelDef = null;
            if (def.defName == "CPOLICE_WearingSWATBeret")
            {
                requiredApparelDef = "Apparel_CPOLICE_SWATBeret";
            }
            else if (def.defName == "CPOLICE_WearingNewPatrolCap")
            {
                requiredApparelDef = "Apparel_CPOLICE_NewPatrolCap";
            }

            if (requiredApparelDef == null)
            {
                return ThoughtState.Inactive;
            }

            foreach (Apparel apparel in p.apparel.WornApparel)
            {
                if (apparel.def.defName == requiredApparelDef)
                {
                    return ThoughtState.ActiveAtStage(0);
                }
            }

            return ThoughtState.Inactive;
        }
    }
}
