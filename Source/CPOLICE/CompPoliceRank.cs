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

            yield return new Command_PoliceRank(pawn, PoliceRankCommandKind.Assign)
            {
                defaultLabel = rank == null ? "授予警衔" : "调整警衔",
                defaultDesc = (rank == null ? "当前没有警衔。" : "当前警衔：" + rank.LabelCap + "。")
                    + "\n\n选择一个警衔，肩章会自动佩戴；也可以撤销警衔。同时选中多人时会一起授衔。",
                icon = rank?.Icon ?? PoliceRankTextures.Assign
            };

            if (rank == null)
            {
                yield break;
            }

            PoliceRankDef higher = PoliceRankUtility.Neighbor(rank, 1);
            Command_PoliceRank promote = new Command_PoliceRank(pawn, PoliceRankCommandKind.Promote)
            {
                defaultLabel = "晋升",
                defaultDesc = higher == null ? "当前警衔：" + rank.LabelCap + "。" : "晋升为：" + higher.LabelCap + "。",
                icon = PoliceRankTextures.Promote
            };
            if (higher == null)
            {
                promote.Disable("已是" + rank.TrackLabel + "最高警衔。");
            }
            yield return promote;

            PoliceRankDef lower = PoliceRankUtility.Neighbor(rank, -1);
            Command_PoliceRank demote = new Command_PoliceRank(pawn, PoliceRankCommandKind.Demote)
            {
                defaultLabel = "降级",
                defaultDesc = lower == null ? "当前警衔：" + rank.LabelCap + "。" : "降级为：" + lower.LabelCap + "。",
                icon = PoliceRankTextures.Demote
            };
            if (lower == null)
            {
                demote.Disable("已是" + rank.TrackLabel + "最低警衔。");
            }
            yield return demote;
        }

        public override string CompInspectStringExtra()
        {
            PoliceRankDef rank = PoliceRankUtility.CurrentRank(Pawn);
            return rank == null ? null : "警衔：" + rank.LabelCap;
        }
    }

    public enum PoliceRankCommandKind
    {
        Assign,
        Promote,
        Demote
    }

    public class Command_PoliceRank : Command_Action
    {
        private readonly Pawn pawn;
        private readonly PoliceRankCommandKind kind;
        private List<Pawn> groupPawns;

        public Command_PoliceRank(Pawn pawn, PoliceRankCommandKind kind)
        {
            this.pawn = pawn;
            this.kind = kind;
            alsoClickIfOtherInGroupClicked = false;
            action = Execute;
        }

        public override bool GroupsWith(Gizmo other)
        {
            return other is Command_PoliceRank command && command.kind == kind && command.disabled == disabled;
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

            switch (kind)
            {
                case PoliceRankCommandKind.Assign:
                    OpenAssignMenu(targets);
                    break;
                case PoliceRankCommandKind.Promote:
                    Step(targets, 1);
                    break;
                case PoliceRankCommandKind.Demote:
                    Step(targets, -1);
                    break;
            }
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

        private static void Step(List<Pawn> targets, int step)
        {
            foreach (Pawn target in targets)
            {
                PoliceRankDef next = PoliceRankUtility.Neighbor(PoliceRankUtility.CurrentRank(target), step);
                if (next == null || !PoliceRankUtility.TrySetRank(target, next))
                {
                    continue;
                }

                string verb = step > 0 ? " 晋升为 " : " 降级为 ";
                Messages.Message(target.LabelShort + verb + next.LabelCap + "。", target,
                    step > 0 ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NeutralEvent, false);
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class PoliceRankTextures
    {
        public static readonly Texture2D Assign = ContentFinder<Texture2D>.Get("UI/CPOLICE/Commands/RankAssign");
        public static readonly Texture2D Promote = ContentFinder<Texture2D>.Get("UI/CPOLICE/Commands/RankPromote");
        public static readonly Texture2D Demote = ContentFinder<Texture2D>.Get("UI/CPOLICE/Commands/RankDemote");
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
