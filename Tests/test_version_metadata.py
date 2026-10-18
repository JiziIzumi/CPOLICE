import unittest
from pathlib import Path
from xml.etree import ElementTree as E

ROOT = Path(__file__).resolve().parents[1]

class VersionMetadata(unittest.TestCase):
    def test_declares_both_supported_game_versions(self):
        about = E.parse(ROOT / 'About' / 'About.xml').getroot()
        supported = [node.text for node in about.findall('./supportedVersions/li')]
        self.assertIn('1.6', supported)
        self.assertIn('1.7', supported)

    def test_17_loads_existing_shared_content_and_ce_patch(self):
        folders = E.parse(ROOT / 'LoadFolders.xml').getroot()
        version = next((node for node in folders if node.tag == 'v1.7'), None)
        self.assertIsNotNone(version, 'missing v1.7 load folder mapping')
        entries = [(node.text, node.attrib.get('IfModActive')) for node in version.findall('./li')]
        self.assertEqual(entries, [
            ('CE/CE1.6', 'CETeam.CombatExtended'),
            ('1.6', None),
            ('/', None),
            ('1.7', None),
        ])

if __name__ == '__main__':
    unittest.main()
