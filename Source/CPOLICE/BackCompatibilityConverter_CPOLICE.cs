using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Verse;

namespace CPOLICE
{
    // RimWorld 1.7 rejects ThingDef names ending in a digit; maps the old names so saved items survive.
    public class BackCompatibilityConverter_CPOLICE : BackCompatibilityConverter
    {
        private static readonly Dictionary<string, string> RenamedThingDefs = new Dictionary<string, string>
        {
            { "CPOLICE_QBZ95", "CPOLICE_QBZ95Rifle" },
            { "CPOLICE_QBZ192", "CPOLICE_QBZ192Rifle" },
            { "CPOLICE_Bullet_58x42_QBZ95", "CPOLICE_Bullet_58x42_QBZ95Rifle" },
            { "CPOLICE_Bullet_58x42_QBZ951", "CPOLICE_Bullet_58x42_QBZ951Rifle" },
            { "CPOLICE_Bullet_58x42_QBZ192", "CPOLICE_Bullet_58x42_QBZ192Rifle" },
            { "CPOLICE_Bullet_58x42_QBU88", "CPOLICE_Bullet_58x42_QBU88Sniper" },
            { "CPOLICE_Bullet_308Win_CSLR4", "CPOLICE_Bullet_308Win_CSLR4Sniper" },
            { "CPOLICE_Bullet_12Gauge_QBS09", "CPOLICE_Bullet_12Gauge_QBS09Shotgun" },
            { "CPOLICE_Bullet_9mm_CQC171", "CPOLICE_Bullet_9mm_CQC171SMG" }
        };

        public override bool AppliesToVersion(int majorVer, int minorVer)
        {
            return true;
        }

        public override string BackCompatibleDefName(Type defType, string defName, bool forDefInjections = false, XmlNode node = null)
        {
            if (defType == typeof(ThingDef) && defName != null && RenamedThingDefs.TryGetValue(defName, out string renamed))
            {
                return renamed;
            }

            return null;
        }

        public override Type GetBackCompatibleType(Type baseType, string providedClassName, XmlNode node)
        {
            return null;
        }

        public override void PostExposeData(object obj)
        {
        }
    }

    [StaticConstructorOnStartup]
    public static class BackCompatibilityRegistration
    {
        static BackCompatibilityRegistration()
        {
            // The vanilla chain is private and has no public hook for mods.
            FieldInfo field = typeof(BackCompatibility).GetField("conversionChain", BindingFlags.NonPublic | BindingFlags.Static);
            if (field?.GetValue(null) is List<BackCompatibilityConverter> chain
                && !chain.Any(converter => converter is BackCompatibilityConverter_CPOLICE))
            {
                chain.Add(new BackCompatibilityConverter_CPOLICE());
            }
        }
    }
}
