import unittest
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


class RuntimeDefReferences(unittest.TestCase):
    def test_shared_definitions_use_the_vanilla_global_work_speed_stat(self):
        for folder in (ROOT / "1.6", ROOT / "CE" / "CE1.6"):
            for path in folder.rglob("*.xml"):
                self.assertNotIn(
                    "GlobalWorkSpeed",
                    path.read_text(encoding="utf-8-sig"),
                    f"{path} references an unknown stat; use WorkSpeedGlobal",
                )

    def test_17_sound_patch_replaces_both_legacy_shotgun_sound_ids(self):
        folders = ET.parse(ROOT / "LoadFolders.xml").getroot()
        v17 = next(node for node in folders if node.tag == "v1.7")
        entries = [node.text for node in v17.findall("./li")]
        self.assertEqual(entries[-1], "1.7", "1.7 override must load after shared and CE content")

        patch_path = ROOT / "1.7" / "Patches" / "CPOLICE_1.7_ShotgunSounds.xml"
        patch = ET.parse(patch_path).getroot()
        replacements = {
            operation.findtext("xpath"): operation.findtext("value/soundCast")
            for operation in patch.findall("./Operation")
            if operation.get("Class") == "PatchOperationReplace"
        }
        for def_name in ("CPOLICE_Type972Shotgun", "CPOLICE_QBS09Shotgun"):
            xpath = f'/Defs/ThingDef[defName="{def_name}"]/verbs/li/soundCast'
            self.assertEqual(replacements.get(xpath), "Shot_Shotgun", f"missing 1.7 sound override for {def_name}")


if __name__ == "__main__":
    unittest.main()
