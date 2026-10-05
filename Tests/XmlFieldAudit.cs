using System;using System.IO;using System.Linq;using System.Reflection;using System.Xml;using System.Collections.Generic;
class XmlAudit {
 static Assembly game,mod,rocket;static int count,err;static string refs;
 static Type Find(string s){return mod.GetType(s)??rocket.GetType(s)??game.GetType(s)??game.GetType("Verse."+s)??game.GetType("RimWorld."+s)??game.GetType("Verse.AI."+s);}
 static FieldInfo Field(Type t,string n){for(;t!=null;t=t.BaseType){var f=t.GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);if(f!=null)return f;}return null;}
 static void Walk(XmlElement node,Type type,string path){
  if(node.HasAttribute("Class")){var t=Find(node.GetAttribute("Class"));if(t==null){Console.WriteLine("EXTERNAL_CLASS "+path+" "+node.GetAttribute("Class"));return;}type=t;}
  if(type==null){Console.WriteLine("EXTERNAL_TYPE "+path);return;}count++;
  if(type.IsPrimitive||type.FullName=="System.String"||type.IsEnum)return;
  if(type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Any(m=>m.Name=="LoadDataFromXmlCustom"))return;
  if(type.IsGenericType){var generic=type.GetGenericTypeDefinition().FullName;
   if(generic=="System.Collections.Generic.List`1"||generic=="System.Collections.Generic.IEnumerable`1"){
    var item=type.GetGenericArguments()[0];if(item.IsSubclassOf(Find("Verse.Def")))return;
    foreach(var c in node.ChildNodes.OfType<XmlElement>())Walk(c,item,path+"/"+c.Name);return;
   }
   if(generic=="System.Collections.Generic.Dictionary`2")return;
  }
  foreach(var c in node.ChildNodes.OfType<XmlElement>()){
   var f=Field(type,c.Name);if(f==null){Console.WriteLine("FAIL_FIELD "+path+"/"+c.Name+" on "+type.FullName);err++;continue;}
   if(f.FieldType.IsSubclassOf(Find("Verse.Def")))continue;
   Walk(c,f.FieldType,path+"/"+c.Name);
  }
 }
 static int Main(string[] args){refs=args[0];AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve+=(s,e)=>{string name=new AssemblyName(e.Name).Name;string p=Path.Combine(refs,name+".dll");if(name=="CPOLICE")p=Path.Combine(args[1],"Assemblies/CPOLICE.dll");return File.Exists(p)?Assembly.ReflectionOnlyLoadFrom(p):Assembly.ReflectionOnlyLoad(e.Name);};game=Assembly.ReflectionOnlyLoadFrom(Path.Combine(refs,"Assembly-CSharp.dll"));mod=Assembly.ReflectionOnlyLoadFrom(Path.Combine(args[1],"Assemblies/CPOLICE.dll"));rocket=Assembly.ReflectionOnlyLoadFrom(args[2]);foreach(var f in Directory.GetFiles(Path.Combine(args[1],"1.6/Defs"),"*.xml")){var d=new XmlDocument();d.Load(f);foreach(var n in d.DocumentElement.ChildNodes.OfType<XmlElement>())Walk(n,Find(n.Name),Path.GetFileName(f)+"/"+n.Name+"["+n.SelectSingleNode("defName")?.InnerText+"]");}Console.WriteLine("SUMMARY "+count+" nodes; "+err+" invalid fields");return err>0?1:0;}
}
