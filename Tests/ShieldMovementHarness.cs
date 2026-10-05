// Minimal dependency stand-ins to execute the actual production StatPart in isolation.
// XML integration and compilation against RimWorld references are checked separately.
using System;
using System.Collections.Generic;
namespace Verse { public class Thing { } public class ThingDef { public string defName; } public class Pawn : Thing { public RimWorld.Pawn_ApparelTracker apparel; } }
namespace RimWorld {
    public class Apparel { public Verse.ThingDef def; }
    public class Pawn_ApparelTracker { public List<Apparel> WornApparel = new List<Apparel>(); }
    public struct StatRequest { public Verse.Thing Thing; public bool HasThing { get { return Thing != null; } } }
    public abstract class StatPart { public abstract void TransformValue(StatRequest req, ref float val); public abstract string ExplanationPart(StatRequest req); }
}
class ShieldMovementHarness {
    static void Check(bool condition,string message) { if (!condition) throw new Exception(message); }
    static void Main() {
        var part = new CPOLICE.StatPart_PoliceShieldMovement();
        var pawn = new Verse.Pawn { apparel = new RimWorld.Pawn_ApparelTracker() };
        var req = new RimWorld.StatRequest { Thing = pawn };
        float value=4.6f; part.TransformValue(req,ref value); Check(Math.Abs(value-4.6f)<0.0001,"Unshielded pawn changed");
        pawn.apparel.WornApparel.Add(new RimWorld.Apparel { def = new Verse.ThingDef { defName="Apparel_CPOLICE_RiotShield" } });
        foreach(float initial in new float[] { 2f,4.6f,8f }) { value=initial;part.TransformValue(req,ref value);Check(Math.Abs(value-initial*0.9f)<0.0001,"Penalty is not 10 percent"); }
        Check(part.ExplanationPart(req)!=null,"Shield explanation missing");
        pawn.apparel.WornApparel.Clear();value=8f;part.TransformValue(req,ref value);Check(value==8f,"Penalty persists after removing shield");
        pawn.apparel=null;part.TransformValue(req,ref value);Check(value==8f,"Pawn without apparel changed");
        part.TransformValue(new RimWorld.StatRequest(),ref value);Check(value==8f,"Empty request changed");
        part.TransformValue(new RimWorld.StatRequest { Thing=new Verse.Thing() },ref value);Check(value==8f,"Non-pawn changed");
        Console.WriteLine("PASS: shield movement at 3 speeds, removal, explanation, empty/non-pawn/no-apparel requests");
    }
}
