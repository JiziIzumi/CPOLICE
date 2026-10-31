using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CPOLICE
{
    public enum PoliceRankTrack
    {
        Auxiliary,
        Police
    }

    public class PoliceRankDef : Def
    {
        public PoliceRankTrack track;
        public int rankLevel;
        public string iconPath;
        public ThingDef insignia;

        private Texture2D iconInt;

        public Texture2D Icon
        {
            get
            {
                if (iconInt == null)
                {
                    iconInt = iconPath.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(iconPath, false);
                    if (iconInt == null)
                    {
                        iconInt = BaseContent.BadTex;
                    }
                }

                return iconInt;
            }
        }

        public string TrackLabel => track == PoliceRankTrack.Auxiliary ? "辅警" : "人民警察";

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (insignia == null)
            {
                yield return "insignia is null";
            }
            else if (insignia.apparel == null)
            {
                yield return "insignia " + insignia.defName + " is not apparel";
            }
        }
    }

    // A pawn's rank is whichever CPOLICE insignia it wears, so there is no extra state to save.
    public static class PoliceRankUtility
    {
        private static List<PoliceRankDef> orderedRanks;
        private static Dictionary<ThingDef, PoliceRankDef> rankByInsignia;

        public static List<PoliceRankDef> AllRanksOrdered
        {
            get
            {
                EnsureCache();
                return orderedRanks;
            }
        }

        public static PoliceRankDef RankForInsignia(ThingDef def)
        {
            EnsureCache();
            return def != null && rankByInsignia.TryGetValue(def, out PoliceRankDef rank) ? rank : null;
        }

        public static PoliceRankDef CurrentRank(Pawn pawn)
        {
            List<Apparel> worn = pawn?.apparel?.WornApparel;
            if (worn == null)
            {
                return null;
            }

            for (int i = 0; i < worn.Count; i++)
            {
                PoliceRankDef rank = RankForInsignia(worn[i].def);
                if (rank != null)
                {
                    return rank;
                }
            }

            return null;
        }

        public static bool TrySetRank(Pawn pawn, PoliceRankDef rank)
        {
            if (pawn?.apparel == null || pawn.Dead)
            {
                return false;
            }

            if (rank != null && !ApparelUtility.HasPartsToWear(pawn, rank.insignia))
            {
                Messages.Message(pawn.LabelShort + " 没有可以佩戴肩章的身体部位。", pawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            RemoveInsignia(pawn);
            if (rank == null)
            {
                return true;
            }

            Apparel insignia = (Apparel)ThingMaker.MakeThing(rank.insignia);
            pawn.apparel.Wear(insignia, false, true);
            return true;
        }

        private static void RemoveInsignia(Pawn pawn)
        {
            List<Apparel> worn = pawn.apparel.WornApparel;
            for (int i = worn.Count - 1; i >= 0; i--)
            {
                Apparel apparel = worn[i];
                if (RankForInsignia(apparel.def) == null)
                {
                    continue;
                }

                pawn.apparel.Unlock(apparel);
                pawn.apparel.Remove(apparel);
                apparel.Destroy();
            }
        }

        private static void EnsureCache()
        {
            if (orderedRanks != null)
            {
                return;
            }

            orderedRanks = DefDatabase<PoliceRankDef>.AllDefsListForReading
                .OrderBy(rank => rank.track)
                .ThenBy(rank => rank.rankLevel)
                .ToList();

            rankByInsignia = new Dictionary<ThingDef, PoliceRankDef>();
            foreach (PoliceRankDef rank in orderedRanks)
            {
                if (rank.insignia != null)
                {
                    rankByInsignia[rank.insignia] = rank;
                }
            }
        }
    }
}
