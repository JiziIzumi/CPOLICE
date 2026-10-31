"""Static integration checks: apply shipped XML operations before checking resolved defs.
Run: python -m unittest discover -s Tests -v (requires lxml).
Does not replace RimWorld runtime testing or XML inheritance resolution.
"""
import copy
import unittest
from pathlib import Path
from lxml import etree as E
ROOT = Path(__file__).resolve().parents[1]

def load(ce):
    root = E.Element('Defs')
    folders = [ROOT/'Common/Defs'] + ([ROOT/'CE/CE1.6/Defs'] if ce else [])
    for folder in folders:
        for path in sorted(folder.rglob('*.xml')):
            for node in E.parse(str(path)).getroot(): root.append(copy.deepcopy(node))
    # Required vanilla target, whose implementation remains owned by RimWorld.
    E.SubElement(E.SubElement(root,'StatDef'),'defName').text='MoveSpeed'
    def apply(op):
        if not isinstance(op.tag,str): return
        cls=op.get('Class'); xp=op.findtext('xpath'); targets=root.xpath(xp) if xp else []
        if cls=='PatchOperationSequence':
            for child in op.find('operations'): apply(child)
        elif cls=='PatchOperationConditional':
            child=op.find('match' if targets else 'nomatch')
            if child is not None: apply(child)
        elif cls=='CombatExtended.PatchOperationMakeGunCECompatible':
            target=root.xpath('ThingDef[defName="'+op.findtext('defName')+'"]')[0]
            for field in ('statBases','weaponTags'):
                value=op.find(field)
                if value is None: continue
                dest=target.find(field)
                if dest is None: dest=E.SubElement(target,field)
                for child in value:
                    for old in dest.findall(child.tag):
                        if field=='statBases':dest.remove(old)
                    dest.append(copy.deepcopy(child))
        elif cls in ('PatchOperationAdd','PatchOperationReplace','PatchOperationRemove','PatchOperationAttributeSet'):
            if not targets: raise AssertionError('Patch target missing: '+str(xp))
            for target in targets:
                if cls=='PatchOperationAttributeSet':target.set(op.findtext('attribute'),op.findtext('value'))
                elif cls=='PatchOperationRemove':target.getparent().remove(target)
                elif cls=='PatchOperationAdd':
                    for child in op.find('value'):target.append(copy.deepcopy(child))
                else:
                    parent=target.getparent();index=parent.index(target);parent.remove(target)
                    for child in op.find('value'):parent.insert(index,copy.deepcopy(child));index+=1
        else:raise AssertionError('Unsupported operation '+str(cls))
    folders=[ROOT/'Common/Patches'] + ([ROOT/'CE/CE1.6/Patches'] if ce else [])
    for folder in folders:
        for path in sorted(folder.glob('*.xml')):
            for op in E.parse(str(path)).getroot(): apply(op)
    return root

class Compatibility(unittest.TestCase):
    def setUp(self):self.ce=load(True);self.vanilla=load(False)
    def thing(self,name,ce=True):return (self.ce if ce else self.vanilla).xpath('ThingDef[defName="'+name+'"]')[0]
    def test_pistols_can_fire_with_ce_shield(self):
        for name in ('CPOLICE_QSZ92G','CPOLICE_QSZ92ATactical'):
            self.assertIn('CE_OneHandedWeapon',self.thing(name).xpath('weaponTags/li/text()'))
    def test_belts_expand_ce_inventory_and_keep_vanilla_carrying(self):
        for name in ('Apparel_CPOLICE_PoliceBelt','Apparel_CPOLICE_ArmedDutyBelt'):
            d=self.thing(name)
            for stat in ('CarryWeight','CarryBulk'):self.assertGreater(float(d.findtext('equippedStatOffsets/'+stat,'0')),0)
            self.assertIsNone(d.find('equippedStatOffsets/CarryingCapacity'))
            self.assertEqual(self.thing(name,False).findtext('equippedStatOffsets/CarryingCapacity'),'50')
    def test_all_apparel_has_explicit_ce_bulk(self):
        for d in self.ce.xpath('ThingDef[apparel and defName]'):
            for stat in ('Bulk','WornBulk'):self.assertIsNotNone(d.find('statBases/'+stat),d.findtext('defName')+' '+stat)
    def test_new_armor_uses_existing_conversion_scale(self):
        for name in ('Apparel_CPOLICE_ArmedDutyBelt','Apparel_CPOLICE_HKPoliceCoat','Apparel_CPOLICE_SWATBeret','Apparel_CPOLICE_NewPatrolCap'):
            for stat,factor in (('ArmorRating_Sharp',20),('ArmorRating_Blunt',40)):
                self.assertAlmostEqual(float(self.thing(name).findtext('statBases/'+stat)),float(self.thing(name,False).findtext('statBases/'+stat))*factor)
    def test_ce_shield_uses_render_nodes(self):
        self.assertTrue(self.thing('Apparel_CPOLICE_RiotShield').xpath('apparel/renderNodeProperties/li[workerClass="CombatExtended.PawnRenderNodeWorker_Drafted"]'))
        self.assertIsNone(self.thing('Apparel_CPOLICE_RiotShield').find('apparel/wornGraphicPath'))
    def test_shield_does_not_apply_flat_speed_penalty(self):
        for ce in (True,False):
            self.assertIsNone(self.thing('Apparel_CPOLICE_RiotShield',ce).find('equippedStatOffsets/MoveSpeed'))
        self.assertEqual(len(self.vanilla.xpath('StatDef[defName="MoveSpeed"]/parts/li[@Class="CPOLICE.StatPart_PoliceShieldMovement"]')),1)
    def test_gun_melee_tools_have_ce_penetration(self):
        for d in self.ce.xpath('ThingDef[verbs and defName]/tools/li'):
            self.assertEqual(d.get('Class'),'CombatExtended.ToolCE')
            self.assertGreater(float(d.findtext('armorPenetrationBlunt','0')),0)
    def test_ce_is_declared_in_load_order(self):
        self.assertIn('CETeam.CombatExtended',E.parse(str(ROOT/'About/About.xml')).xpath('//loadAfter/li/text()'))
    def test_all_xml_parses_and_defs_are_unique(self):
        for path in ROOT.rglob('*.xml'): E.parse(str(path))
        names=self.ce.xpath('*/defName/text()');self.assertEqual(len(names),len(set(names)))
    def test_ammo_links_and_all_guns_are_patched(self):
        names=set(self.ce.xpath('*/defName/text()'))
        sets={d.findtext('defName'):d for d in self.ce if d.tag=='CombatExtended.AmmoSetDef'}
        for s in sets.values():
            for link in s.find('ammoTypes'):self.assertIn(link.tag,names);self.assertIn(link.text,names)
        patched=[]
        for path in (ROOT/'CE/CE1.6/Patches').glob('*.xml'):
            for op in E.parse(str(path)).xpath('//*[@Class="CombatExtended.PatchOperationMakeGunCECompatible"]'):
                patched.append(op.findtext('defName'));self.assertIn(op.findtext('AmmoUser/ammoSet'),sets)
                self.assertIn(op.findtext('Properties/defaultProjectile'),names)
        guns=self.ce.xpath('ThingDef[verbs and defName]/defName/text()');self.assertEqual(set(guns),set(patched))
    def test_no_rockets_ranks_dependency(self):
        about=E.parse(str(ROOT/'About/About.xml'))
        self.assertFalse(about.xpath('//modDependencies/li[packageId="rockets.ranks"]'))
        self.assertNotIn('rockets.ranks',about.xpath('//loadAfter/li/text()'))
        for path in list((ROOT/'Common').rglob('*.xml'))+list((ROOT/'CE').rglob('*.xml')):
            self.assertNotIn('Rocket',path.read_text(encoding='utf-8'),str(path))
    def test_every_rank_has_its_own_insignia(self):
        ranks=self.vanilla.xpath('*[local-name()="CPOLICE.PoliceRankDef"]')
        self.assertEqual(len(ranks),21)
        insignia=[r.findtext('insignia') for r in ranks]
        self.assertEqual(len(insignia),len(set(insignia)))
        for rank in ranks:
            self.assertTrue(self.vanilla.xpath('ThingDef[defName="'+rank.findtext('insignia')+'"]'),rank.findtext('defName'))
            code=rank.findtext('defName').split('_')[-1]
            self.assertEqual(rank.findtext('track'),'Auxiliary' if code.startswith('F') else 'Police')
            self.assertTrue((ROOT/'Textures'/(rank.findtext('iconPath')+'.png')).exists())
    def test_insignia_render_through_own_nodes(self):
        base=self.vanilla.xpath('ThingDef[@Name="CPOLICE_RankInsigniaBase"]')[0]
        node=base.find('apparel/renderNodeProperties/li')
        self.assertEqual(node.findtext('nodeClass'),'CPOLICE.PawnRenderNode_PoliceInsignia')
        self.assertEqual(node.findtext('workerClass'),'CPOLICE.PawnRenderNodeWorker_PoliceInsignia')
        self.assertEqual(len(base.xpath('modExtensions/li[@Class="CPOLICE.InsigniaRenderExtension"]/bodyTypes/li')),5)
        # Head is render layer 50: the base layer must start inside the body-apparel range.
        self.assertLess(float(node.findtext('baseLayer')),50)
        for d in self.vanilla.xpath('ThingDef[starts-with(defName,"Apparel_CPOLICE_Rank_")]'):
            self.assertIn(d.get('ParentName'),('CPOLICE_RankInsignia_Enlisted','CPOLICE_RankInsignia_Officer'))
            self.assertIsNone(d.find('apparel/wornGraphicPath'))
            tex=d.findtext('graphicData/texPath')
            for suffix in ('','_south','_east'):self.assertTrue((ROOT/'Textures'/(tex+suffix+'.png')).exists(),tex+suffix)
    def test_xml_classes_exist_in_source(self):
        source=''.join(p.read_text(encoding='utf-8') for p in (ROOT/'Source/CPOLICE').glob('*.cs'))
        names=set()
        for path in list((ROOT/'Common').rglob('*.xml'))+list((ROOT/'CE').rglob('*.xml')):
            tree=E.parse(str(path))
            names.update(v.split('.',1)[1] for v in tree.xpath('//@Class|//nodeClass/text()|//workerClass/text()') if v.startswith('CPOLICE.'))
            names.update(el.tag.split('.',1)[1] for el in tree.iter() if isinstance(el.tag,str) and el.tag.startswith('CPOLICE.'))
        self.assertTrue(names)
        for name in names:self.assertRegex(source,r'class '+name+r'\b',name)
    def test_thingdef_names_do_not_end_with_digit(self):
        # RimWorld 1.7 rejects these at load time.
        for name in self.ce.xpath('ThingDef/defName/text()'):self.assertFalse(name[-1].isdigit(),name)
    def test_each_game_version_loads_its_own_assembly(self):
        about=E.parse(str(ROOT/'About/About.xml'))
        folders=E.parse(str(ROOT/'LoadFolders.xml')).getroot()
        for version in ('1.6','1.7'):
            self.assertIn(version,about.xpath('//supportedVersions/li/text()'))
            loaded=folders.find('v'+version).xpath('li/text()')
            self.assertIn('Common',loaded);self.assertIn(version,loaded)
            self.assertFalse(set(loaded)&({'1.6','1.7'}-{version}))
        self.assertFalse((ROOT/'Assemblies').exists())
if __name__=='__main__':unittest.main()
