// Executes production shoulder-light components with minimal game dependencies.
using System;
using System.Collections.Generic;
using System.Reflection;
using CPOLICE;
namespace Verse {
 public class ThingDef { public object uiIcon; }
 public class Thing { public ThingDef def=new ThingDef(); }
 public class CompProperties { public Type compClass; }
 public class ThingComp { public Thing parent; public CompProperties props; public virtual void PostExposeData(){} public virtual IEnumerable<Gizmo> CompGetWornGizmosExtra(){yield break;} public virtual void Notify_Equipped(Pawn p){} public virtual void Notify_Unequipped(Pawn p){} public virtual void Notify_WearerDied(){} public virtual void Notify_Downed(){} public virtual void PostDeSpawn(Map m,DestroyMode d=DestroyMode.Vanish){} public virtual void PostDestroy(DestroyMode d,Map m){} }
 public enum DestroyMode { Vanish } public enum LoadSaveMode { PostLoadInit }
 public static class Scribe {public static LoadSaveMode mode;} public static class Scribe_Values {public static void Look<T>(ref T v,string k,T d){} }
 public class Gizmo{} public class Command_Action:Gizmo { public string defaultLabel,defaultDesc;public object icon;public Action action; }
 public class Faction { public static Faction OfPlayer=new Faction(); }
 public struct IntVec3 {public int x; public static IntVec3 Invalid=new IntVec3 {x=-1};public bool IsValid=>x>=0;public bool InBounds(Map m)=>IsValid;public static bool operator ==(IntVec3 a,IntVec3 b)=>a.x==b.x;public static bool operator !=(IntVec3 a,IntVec3 b)=>a.x!=b.x;public override bool Equals(object o)=>o is IntVec3 && this==(IntVec3)o;public override int GetHashCode()=>x; }
 public struct ColorInt {public ColorInt(int r,int g,int b){} }
 public class Pawn:Thing {public Map Map;public bool Spawned=true,Dead,Downed;public Faction Faction=Faction.OfPlayer;public IntVec3 Position;public RimWorld.Pawn_ApparelTracker apparel=new RimWorld.Pawn_ApparelTracker();}
 public class MapComponent {protected Map map;public MapComponent(Map m){map=m;}public virtual void FinalizeInit(){}public virtual void MapComponentTick(){}public virtual void MapRemoved(){} }
 public class Map {public MapComponent_PoliceStrobe component;public GlowGrid glowGrid=new GlowGrid();public MapDrawer mapDrawer=new MapDrawer();public MapPawns mapPawns=new MapPawns();public Map(){component=new MapComponent_PoliceStrobe(this);}public T GetComponent<T>() where T:class=>component as T;}
 public class MapPawns {public List<Pawn> AllPawnsSpawned=new List<Pawn>();}
 public class MapDrawer {public void MapMeshDirty(IntVec3 p,object f){} }
 public class GlowGrid {public HashSet<RimWorld.CompGlower> lights=new HashSet<RimWorld.CompGlower>();public void RegisterGlower(RimWorld.CompGlower c){lights.Add(c);}public void DeRegisterGlower(RimWorld.CompGlower c){lights.Remove(c);} }
 public static class Find {public static TickManager TickManager=new TickManager();} public class TickManager {public int TicksGame=100;}
}
namespace RimWorld {
 public class Apparel:Verse.Thing {public Verse.Pawn Wearer;public CompPoliceStrobe comp;public T GetComp<T>() where T:class=>comp as T;}
 public class Pawn_ApparelTracker {public List<Apparel> WornApparel=new List<Apparel>();}
 public class CompProperties_Glower:Verse.CompProperties {public float glowRadius,overlightRadius;public Verse.ColorInt glowColor;}
 public class CompGlower:Verse.ThingComp {public void Initialize(CompProperties_Glower p){props=p;}}
 public static class MapMeshFlagDefOf {public static object Things=new object();}
}
class StrobeLifecycleHarness {
 static void Check(bool c,string m){if(!c)throw new Exception(m);}
 static int Active(Verse.Map m)=>((HashSet<CompPoliceStrobe>)typeof(MapComponent_PoliceStrobe).GetField("active",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m.component)).Count;
 static CompPoliceStrobe Lamp(Verse.Pawn p){var a=new RimWorld.Apparel {Wearer=p};var c=new CompPoliceStrobe {parent=a,props=new CompProperties_PoliceStrobe()};a.comp=c;p.apparel.WornApparel.Add(a);return c;}
 static void Cycle(CompPoliceStrobe c){foreach(var g in c.CompGetWornGizmosExtra())if(g is Verse.Command_Action)((Verse.Command_Action)g).action();}
 static void Main(){
 var a=new Verse.Map();var b=new Verse.Map();var p=new Verse.Pawn {Map=a};var c=Lamp(p);Cycle(c);Check(a.glowGrid.lights.Count==1,"White lamp missing");
 p.Position=new Verse.IntVec3 {x=2};a.component.MapComponentTick();Check(a.glowGrid.lights.Count==1,"Movement duplicates light");
 ((RimWorld.Apparel)c.parent).Wearer=null;c.Notify_Unequipped(p);Check(Active(a)==0,"Unequip leaves stale map registration");Check(a.glowGrid.lights.Count==0,"Unequip leaves glow");
 ((RimWorld.Apparel)c.parent).Wearer=p;Cycle(c);p.Map=b;a.component.MapComponentTick();Check(Active(a)==0 && Active(b)==1,"Map transfer does not migrate registration");Check(a.glowGrid.lights.Count==0 && b.glowGrid.lights.Count==1,"Transfer glow incorrect");a.component.MapRemoved();Check(b.glowGrid.lights.Count==1,"Old map removal extinguishes new map lamp");
 p.Downed=true;b.component.MapComponentTick();Check(!c.IsActive && Active(b)==0 && b.glowGrid.lights.Count==0,"Downing fails cleanup");p.Downed=false;Cycle(c);Cycle(c);for(int i=0;i<32;i++){Verse.Find.TickManager.TicksGame+=45;b.component.MapComponentTick();Check(b.glowGrid.lights.Count<=1,"Strobe accumulates lights");}Cycle(c);Check(Active(b)==0 && b.glowGrid.lights.Count==0,"Off fails cleanup");
 var d=new Verse.Map();p.Map=b;Cycle(c);p.Map=d;b.component.MapRemoved();Check(Active(d)==1 && d.glowGrid.lights.Count==1,"Map removed before next tick loses transferred lamp");d.component.MapRemoved();Check(d.glowGrid.lights.Count==0 && !c.IsActive,"Removed map retains mode");
 Console.WriteLine("PASS: white, movement, unequip, map migration/removal, downed, 32 strobe transitions, off");
 }
}
