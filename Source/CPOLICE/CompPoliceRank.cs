using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CPOLICE
{
    public class CompProperties_PoliceRank : CompProperties
    {
        public CompProperties_PoliceRank()
        {
            compClass = typeof(CompPoliceRank);
        }
    }

    // Lives on every humanlike pawn (added at startup) so the rank buttons need no Harmony patch.
    public class CompPoliceRank : ThingComp
    {
        private Pawn Pawn => parent as Pawn;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            Pawn pawn = Pawn;
            if (pawn?.apparel == null || !pawn.IsColonistPlayerControlled || PoliceRankUtility.AllRanksOrdered.Count == 0)
            {
                yield break;
            }

            PoliceRankDef rank = PoliceRankUtility.CurrentRank(pawn);

            yield return new Command_PoliceRank(pawn)
            {
                defaultLabel = rank == null ? "授予警衔" : "调整警衔",
                defaultDesc = (rank == null ? "当前没有警衔。" : "当前警衔：" + rank.LabelCap + "。")
                    + "\n\n选择一个警衔，肩章会自动佩戴；也可以撤销警衔。同时选中多人时会一起授衔。",
                icon = rank?.Icon ?? PoliceRankTextures.Assign
            };
        }

        public override string CompInspectStringExtra()
        {
            PoliceRankDef rank = PoliceRankUtility.CurrentRank(Pawn);
            return rank == null ? null : "警衔：" + rank.LabelCap;
        }
    }

    public class Command_PoliceRank : Command_Action
    {
        private readonly Pawn pawn;
        private List<Pawn> groupPawns;

        public Command_PoliceRank(Pawn pawn)
        {
            this.pawn = pawn;
            alsoClickIfOtherInGroupClicked = false;
            action = Execute;
        }

        public override bool GroupsWith(Gizmo other)
        {
            return other is Command_PoliceRank;
        }

        // One click on a grouped button acts on every selected pawn exactly once.
        public override void ProcessGroupInput(Event ev, List<Gizmo> group)
        {
            groupPawns = new List<Pawn>();
            foreach (Gizmo gizmo in group)
            {
                if (gizmo is Command_PoliceRank command && command.pawn != null && !groupPawns.Contains(command.pawn))
                {
                    groupPawns.Add(command.pawn);
                }
            }

            try
            {
                ProcessInput(ev);
            }
            finally
            {
                groupPawns = null;
            }
        }

        private void Execute()
        {
            List<Pawn> targets = groupPawns != null && groupPawns.Count > 0 ? new List<Pawn>(groupPawns) : new List<Pawn> { pawn };
            OpenAssignMenu(targets);
        }

        private static void OpenAssignMenu(List<Pawn> targets)
        {
            PoliceRankDef current = targets.Count == 1 ? PoliceRankUtility.CurrentRank(targets[0]) : null;
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            foreach (PoliceRankDef rank in PoliceRankUtility.AllRanksOrdered)
            {
                PoliceRankDef chosen = rank;
                string label = "[" + rank.TrackLabel + "] " + rank.LabelCap + (rank == current ? "（当前）" : "");
                options.Add(new FloatMenuOption(label, () => Assign(targets, chosen), rank.Icon, Color.white));
            }

            bool anyRanked = false;
            foreach (Pawn target in targets)
            {
                if (PoliceRankUtility.CurrentRank(target) != null)
                {
                    anyRanked = true;
                    break;
                }
            }

            if (anyRanked)
            {
                options.Add(new FloatMenuOption("撤销警衔", () => Assign(targets, null)));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void Assign(List<Pawn> targets, PoliceRankDef rank)
        {
            foreach (Pawn target in targets)
            {
                PoliceRankDef before = PoliceRankUtility.CurrentRank(target);
                if (before == rank || !PoliceRankUtility.TrySetRank(target, rank))
                {
                    continue;
                }

                string text = rank == null
                    ? target.LabelShort + " 的警衔已撤销。"
                    : target.LabelShort + " 被授予警衔：" + rank.LabelCap + "。";
                Messages.Message(text, target, MessageTypeDefOf.PositiveEvent, false);
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class PoliceRankTextures
    {
        public static readonly Texture2D Assign = ContentFinder<Texture2D>.Get("UI/CPOLICE/Commands/RankAssign");
    }

    [StaticConstructorOnStartup]
    public static class PoliceRankStartup
    {
        static PoliceRankStartup()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.race == null || !def.race.Humanlike)
                {
                    continue;
                }

                if (def.comps == null)
                {
                    def.comps = new List<CompProperties>();
                }

                if (!def.comps.Any(props => props is CompProperties_PoliceRank))
                {
                    def.comps.Add(new CompProperties_PoliceRank());
                }
            }
        }
    }
}
