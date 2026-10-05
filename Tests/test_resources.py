"""Repository-owned assets and inheritance graph checks; external assets excluded."""
import unittest
from pathlib import Path
from lxml import etree as E
ROOT=Path(__file__).resolve().parents[1]
class Resources(unittest.TestCase):
 def test_custom_asset_paths_exist(self):
  assets=list((ROOT/'Textures').rglob('*.png'))
  for p in ROOT.rglob('*.xml'):
   for path in E.parse(str(p)).xpath('//texPath/text() | //wornGraphicPath/text() | //iconPath/text()'):
    if 'CPOLICE' not in path:continue
    self.assertTrue(any(a.relative_to(ROOT/'Textures').as_posix().startswith(path) for a in assets),str(p)+': '+path)
 def test_custom_parent_graph_is_acyclic(self):
  parents={d.get('Name'):d.get('ParentName') for p in ROOT.rglob('*.xml') for d in E.parse(str(p)).xpath('/Defs/*[@Name]')}
  for name in parents:
   seen=set();n=name
   while n in parents:
    self.assertNotIn(n,seen,'inheritance cycle '+name);seen.add(n);n=parents[n]
 def test_no_duplicate_single_fields(self):
  for p in ROOT.rglob('*.xml'):
   for node in E.parse(str(p)).xpath('/Defs/*/descendant-or-self::*'):
    if not isinstance(node.tag,str):continue
    tags=[c.tag for c in node if isinstance(c.tag,str) and c.tag!='li']
    self.assertEqual(len(tags),len(set(tags)),str(p)+': '+node.tag)
