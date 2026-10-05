using RimWorld;
using Verse;

namespace CPOLICE
{
    // A percentage penalty must transform the pawn stat, not use a flat apparel offset.
    public class StatPart_PoliceShieldMovement : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (WearsShield(req)) val *= 0.9f;
        }

        public override string ExplanationPart(StatRequest req)
        {
            return WearsShield(req) ? "通用｜防暴盾牌：移动速度 ×90%" : null;
        }

        private static bool WearsShield(StatRequest req)
        {
            Pawn pawn = req.HasThing ? req.Thing as Pawn : null;
            if (pawn?.apparel == null) return false;
            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                if (apparel.def.defName == "Apparel_CPOLICE_RiotShield") return true;
            }
            return false;
        }
    }
}
