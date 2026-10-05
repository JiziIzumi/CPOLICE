"""Focused checks of custom ammo inheritance; CE's public class names are fixed fixtures."""
import unittest
from pathlib import Path
from lxml import etree as E
ROOT=Path(__file__).resolve().parents[1]
class AmmoInheritance(unittest.TestCase):
 def setUp(self):
  self.defs=[d for p in (ROOT/'CE/CE1.6/Defs/Ammo').glob('*.xml') for d in E.parse(str(p)).getroot() if isinstance(d.tag,str)]
  self.parents={d.get('Name'):d for d in self.defs if d.get('Name')}
 def test_known_ce_ammo_classes(self):
  for d in self.defs:
   if d.find('ammoClass') is not None:self.assertIn(d.findtext('ammoClass'),('FullMetalJacket','BuckShot'))
 def test_category_not_duplicated_by_list_inheritance(self):
  for d in self.defs:
   if d.get('Class')!='CombatExtended.AmmoDef' or d.get('Abstract')=='True':continue
   cats=d.xpath('thingCategories/li/text()');p=self.parents.get(d.get('ParentName'))
   if p is not None and (d.find('thingCategories') is None or d.find('thingCategories').get('Inherit')!='False'):cats=p.xpath('thingCategories/li/text()')+cats
   self.assertEqual(cats.count('CPOLICE_PoliceAmmo'),1,d.findtext('defName'))
 def test_buckshot_has_pellets_and_keeps_total_damage(self):
  p=next(d.find('projectile') for d in self.defs if d.findtext('defName')=='Bullet_12GaugeCPOLICE_Buck')
  self.assertEqual(int(p.findtext('pelletCount','1')),9)
  self.assertEqual(int(p.findtext('damageAmountBase'))*int(p.findtext('pelletCount','1')),36)
  self.assertGreater(float(p.findtext('spreadMult','1')),1)
