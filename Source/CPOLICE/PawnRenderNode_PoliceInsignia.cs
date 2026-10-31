using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CPOLICE
{
    public class InsigniaBodyOffset
    {
        public string bodyType;
        public float northOffsetX;
        public float northOffsetZ;
        public float northScale = 1f;
        public float eastOffsetX;
        public float eastOffsetZ;
        public float eastScale = 1f;
    }

    // Per-body-type shoulder placement, tuned to match the positions used under Rocket's Ranks.
    public class InsigniaRenderExtension : DefModExtension
    {
        public List<InsigniaBodyOffset> bodyTypes;

        public static void GetOffsetAndScale(ThingDef apparelDef, BodyTypeDef bodyType, Rot4 facing,
            out float offsetX, out float offsetZ, out float scale)
        {
            offsetX = 0f;
            offsetZ = 0f;
            scale = 1f;

            List<InsigniaBodyOffset> entries = apparelDef?.GetModExtension<InsigniaRenderExtension>()?.bodyTypes;
            if (entries == null || bodyType == null)
            {
                return;
            }

            bool northSouth = facing == Rot4.North || facing == Rot4.South;
            foreach (InsigniaBodyOffset entry in entries)
            {
                if (entry.bodyType != bodyType.defName)
                {
                    continue;
                }

                offsetX = northSouth ? entry.northOffsetX : entry.eastOffsetX;
                offsetZ = northSouth ? entry.northOffsetZ : entry.eastOffsetZ;
                scale = northSouth ? entry.northScale : entry.eastScale;
                return;
            }
        }
    }

    // One directional texture set for every body type, instead of vanilla's per-body-type worn textures.
    public class PawnRenderNode_PoliceInsignia : PawnRenderNode_Apparel
    {
        public PawnRenderNode_PoliceInsignia(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            string path = apparel?.def.graphicData?.texPath;
            if (path.NullOrEmpty())
            {
                return null;
            }

            return GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.CutoutComplex, Vector2.one,
                apparel.DrawColor, apparel.DrawColorTwo);
        }

        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            Graphic graphic = GraphicFor(pawn);
            if (graphic != null)
            {
                yield return graphic;
            }
        }
    }

    public class PawnRenderNodeWorker_PoliceInsignia : PawnRenderNodeWorker_Apparel_Body
    {
        private const float BaseScale = 0.35f;
        private const float BaseZ = -0.1f;
        private const float SideShiftX = 0.02f;
        private const float ClothesGap = 0.5f;
        private const float HeadGap = 0.5f;

        // Draw order comes only from LayerFor: one layer is ~0.00037 altitude, so any y nudge here
        // (Rocket's +0.01 was ~27 layers) would lift the insignia over the face.
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 offset = base.OffsetFor(node, parms, out pivot);
            InsigniaRenderExtension.GetOffsetAndScale(node.apparel?.def, parms.pawn?.story?.bodyType, parms.facing,
                out float bodyOffsetX, out float bodyOffsetZ, out _);

            offset.z += BaseZ + bodyOffsetZ;

            switch (parms.facing.AsInt)
            {
                case 1:
                    offset.x += SideShiftX + bodyOffsetX;
                    break;
                case 3:
                    offset.x -= SideShiftX + bodyOffsetX;
                    break;
                default:
                    offset.x += bodyOffsetX;
                    break;
            }

            return offset;
        }

        // Vanilla gives body apparel layer 20 + n, lifts shell coats (88) and packs (93) above the head when
        // facing north, and draws the head at 50 with hats from 70. The insignia sits just above whatever the
        // pawn actually wears; except from behind, it stays under the head so the face and hats cover it.
        public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
        {
            float layer = base.LayerFor(node, parms);

            PawnRenderNode[] siblings = node.parent?.children;
            if (siblings != null)
            {
                foreach (PawnRenderNode sibling in siblings)
                {
                    if (sibling == null || sibling == node || sibling.apparel == null)
                    {
                        continue;
                    }

                    layer = Mathf.Max(layer, sibling.Worker.LayerFor(sibling, parms) + ClothesGap);
                }
            }

            if (parms.facing != Rot4.North && node.tree != null
                && node.tree.TryGetNodeByTag(PawnRenderNodeTagDefOf.Head, out PawnRenderNode head))
            {
                layer = Mathf.Min(layer, head.Worker.LayerFor(head, parms) - HeadGap);
            }

            return layer;
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            InsigniaRenderExtension.GetOffsetAndScale(node.apparel?.def, parms.pawn?.story?.bodyType, parms.facing,
                out _, out _, out float scale);
            return base.ScaleFor(node, parms) * (BaseScale * scale);
        }
    }
}
