"""Copy current product GUI renders without changing their pixels."""
from pathlib import Path
import argparse
import shutil

parser=argparse.ArgumentParser()
parser.add_argument("captures",type=Path)
args=parser.parse_args()
out=Path("docs/media/v1.14.0")
(out/"settings").mkdir(parents=True,exist_ok=True)
for source in (args.captures/"light"/"settings").glob("settings-*.png"):
    shutil.copyfile(source,out/"settings"/source.name.removeprefix("settings-"))
for language in ["en","zh-CN"]:
    for page,name in [(1,"timer"),(2,"behavior"),(3,"appearance"),(4,"remote"),(5,"controls"),(6,"other")]:
        source=args.captures/"light"/"settings"/f"settings-{language}-{page:02}-{name}-part-1.png"
        shutil.copyfile(source,out/f"settings-{language}-{name}.png")
for source,name in [("remote-connection","pc-remote-connection"),
                    ("remote-presentation","pc-remote-rules"),
                    ("timer-v1140-idle","timer-idle"),
                    ("timer-v1140-slide","timer-slide"),
                    ("update-notes-top","update-notes"),
                    ("update-notes-end","update-notes-end")]:
    shutil.copyfile(args.captures/"light"/"windows"/f"{source}.png",out/f"{name}.png")
