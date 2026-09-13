"""Select documentation captures, keeping source pixels intact."""
from pathlib import Path
import argparse
import shutil

parser = argparse.ArgumentParser()
parser.add_argument("captures", type=Path)
args = parser.parse_args()
out = Path("docs/media/v1.13.1")
out.mkdir(parents=True, exist_ok=True)
for language in ["en", "zh-CN"]:
    for page, name in [(1,"timer"),(2,"behavior"),(3,"appearance"),(4,"remote"),(5,"controls"),(6,"other")]:
        source = args.captures/"light"/"settings"/f"settings-{language}-{page:02}-{name}-part-1.png"
        shutil.copyfile(source, out/f"settings-{language}-{name}.png")
    shutil.copyfile(args.captures/"dark"/"settings"/f"settings-{language}-03-appearance-part-3.png",
                   out/f"settings-{language}-dark-opacity.png")
for source, name in [("remote-connection","pc-remote-connection"),
                     ("remote-presentation","pc-remote-rules"),
                     ("timer-rc34-default","timer-default")]:
    shutil.copyfile(args.captures/"light"/"windows"/f"{source}.png", out/f"{name}.png")
