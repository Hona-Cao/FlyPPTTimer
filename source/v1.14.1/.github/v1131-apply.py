"""Apply the v1.13.1 changes to the accepted RC3.4 source."""
from pathlib import Path
import json
import re


def edit(file, old, new, count=1):
    p = Path(file)
    s = p.read_text(encoding='utf-8')
    if s.count(old) < count:
        raise ValueError(f'{file}: source anchor not found: {old[:60]}')
    p.write_text(s.replace(old, new, count), encoding='utf-8', newline='\n')

# Mobile: page swipes also start in the list; only freeze the track for horizontal movement.
f = 'src/FlyPPTTimer/Web/app.js'
edit(f, "event.target.closest('.confirm-panel,.presentation-list')", "event.target.closest('.confirm-panel,input,select,textarea')")
edit(f, 'baseX:freezeTrack()', 'baseX:readTrackX()')
p = Path(f)
s = p.read_text(encoding='utf-8')
s = s.replace('if(ay>ax){swipeStart=null;renderPage(false);return}', 'if(ay>ax){swipeStart=null;return}')
s = s.replace('swipeStart.dragging=true;', 'swipeStart.baseX=freezeTrack();swipeStart.dragging=true;', 1)
start = s.index("listHost.addEventListener('touchstart'")
end = s.index("listHost.addEventListener('pointerdown'", start)
s = s[:start] + '''// Native scrolling stays inside the list; transfer finger movement at either boundary.
let listPan=null;
function scrollListBoundary(x,y,event){
  const pan=listPan;if(!pan)return;
  const delta=pan.lastY-y;pan.lastY=y;
  if(listGesture?.active||Math.abs(y-pan.y)<10||Math.abs(y-pan.y)<=Math.abs(x-pan.x))return;
  const atBoundary=delta<0?listHost.scrollTop<=0:listHost.scrollTop+listHost.clientHeight>=listHost.scrollHeight-1;
  if(atBoundary){if(event.cancelable)event.preventDefault();window.scrollBy(0,delta)}
}
listHost.addEventListener('touchstart',event=>{
  if(event.touches.length!==1){listPan=null;finishListGesture(true);return}
  const t=event.touches[0];listPan={x:t.clientX,y:t.clientY,lastY:t.clientY};startListGesture(event.target,t.clientX,t.clientY);
},{passive:true});
listHost.addEventListener('touchmove',event=>{
  if(event.touches.length!==1){listPan=null;finishListGesture(true);return}
  const t=event.touches[0];moveListGesture(t.clientX,t.clientY,event);scrollListBoundary(t.clientX,t.clientY,event);
},{passive:false});
listHost.addEventListener('touchend',()=>{listPan=null;finishListGesture(false)},{passive:true});
listHost.addEventListener('touchcancel',()=>{listPan=null;finishListGesture(true)},{passive:true});
''' + s[end:]
p.write_text(s, encoding='utf-8', newline='\n')
edit('src/FlyPPTTimer/Web/app.css', 'cursor:grab; touch-action:none;', 'cursor:grab; touch-action:pan-y;')

# Visibility is reset before constructing any timer windows.
edit('src/app.rs', '        let mut startup_config = config.borrow_mut();\n', '        let mut startup_config = config.borrow_mut();\n        // A new process always shows the timer, even when the previous session hid it.\n        startup_config.placement.visible = true;\n')
# Settings never owns the lifetime of the timer/tray/Remote service.
for file in ['src/app.rs', 'src/settings.rs']:
    p = Path(file)
    s = p.read_text(encoding='utf-8').replace('    exit_on_close: bool,\n', '').replace('        exit_on_close,\n', '')
    s = re.sub(r'\s*if exit_on_close \{\s*let _\s*=\s*slint::quit_event_loop\(\);\s*\}', '', s)
    p.write_text(s, encoding='utf-8', newline='\n')
edit('src/app.rs', '            Rc::clone(&display_rebuild),\n            true,', '            Rc::clone(&display_rebuild),')
edit('src/app.rs', '                Rc::clone(display_rebuild),\n                false,', '                Rc::clone(display_rebuild),')
for file in ['src/app.rs', 'src/capture.rs']:
    edit(file, '            Rc::new(|| {}),\n            false,', '            Rc::new(|| {}),')

p = Path('ui/app-window.slint')
s = p.read_text(encoding='utf-8')
position = s.index('component RemoteButton')
s = s[:position] + '''// Keep incomplete numeric input local, while valid edits preview immediately.
component PercentageInput inherits LineEdit {
    in property <float> value;
    in property <float> minimum;
    in property <float> maximum;
    in property <float> step: 1;
    callback value-edited(float);
    text: "" + root.value;
    input-type: decimal;
    horizontal-alignment: right;
    changed value => { if !self.has-focus { self.text = "" + root.value; } }
    edited(text) => {
        if text.is-float() {
            let value = text.to-float();
            if value >= root.minimum && value <= root.maximum {
                root.value-edited(round(value / root.step) * root.step);
            }
        }
    }
    accepted(text) => { self.clear-focus(); }
    changed has-focus => {
        if !self.has-focus {
            let value = self.text.is-float() ? self.text.to-float() : root.value;
            let normalized = clamp(round(value / root.step) * root.step, root.minimum, root.maximum);
            self.text = "" + normalized;
            root.value-edited(normalized);
        }
    }
}

''' + s[position:]
old = '''        percent := Slider {
            minimum: item.minimum; maximum: item.maximum; step: item.step;
            value: item.value.to-float(); horizontal-stretch: 1;
            enabled: item.enabled && root.interactive;
            changed(value) => {
                root.edited(row-index, "" + (round(value / item.step) * item.step), false, 0);
            }
        }
        Text {
            width: 58px; text: "" + (round(percent.value * 10) / 10) + "%";
            color: DesktopTheme.text; horizontal-alignment: right; vertical-alignment: center;
        }'''
new = '''        TouchArea {
            horizontal-stretch: 1;
            enabled: item.enabled && root.interactive;
            scroll-event(event) => {
                let delta = event.delta-y != 0px ? event.delta-y : event.delta-x;
                if delta == 0px { return reject; }
                let value = clamp(percent.value + (delta > 0px ? 1 : -1), item.minimum, item.maximum);
                root.edited(row-index, "" + (round(value / item.step) * item.step), false, 0);
                return accept;
            }
            percent := Slider {
                width: parent.width; height: parent.height;
                minimum: item.minimum; maximum: item.maximum; step: item.step;
                value: item.value.to-float();
                enabled: item.enabled && root.interactive;
                changed(value) => {
                    root.edited(row-index, "" + (round(value / item.step) * item.step), false, 0);
                }
            }
        }
        PercentageInput {
            width: 72px; value: item.value.to-float();
            minimum: item.minimum; maximum: item.maximum; step: item.step;
            enabled: item.enabled && root.interactive;
            value-edited(value) => { root.edited(row-index, "" + value, false, 0); }
        }
        Text { width: 12px; text: "%"; color: DesktopTheme.text; vertical-alignment: center; }'''
if old not in s:
    raise ValueError('percentage row source not found')
s = s.replace(old, new, 1)
s = s.replace('    in property <string> rule-duration-text: "时长";', '    in property <string> rule-file-text: "文件名 / 文件路径";\n    in property <string> rule-duration-text: "时长";', 1)
a = s.index('        Text { x: 16px; y: 12px; width: 160px; height: 18px; text: root.rule-duration-text')
b = s.index('        Rectangle {\n            x: 8px; y: 34px;', a)
s = s[:a] + '''        // Header and rows share the data viewport width and scrollbar reservation.
        Rectangle {
            x: 26px; y: 12px; width: rules-scroll.visible-width - 54px; height: 18px;
            Text { x: 12px; width: parent.width - 244px; height: 18px; text: root.rule-file-text; color: DesktopTheme.muted; font-size: 11px; vertical-alignment: center; overflow: elide; }
            Text { x: parent.width - 220px; width: 84px; height: 18px; text: root.rule-duration-text; color: DesktopTheme.muted; font-size: 11px; horizontal-alignment: right; vertical-alignment: center; }
            Text { x: parent.width - 120px; width: 100px; height: 18px; text: root.rule-mode-text; color: DesktopTheme.muted; font-size: 11px; horizontal-alignment: right; vertical-alignment: center; }
        }
''' + s[b:]
a = s.index('            rules-scroll := ScrollView {')
b = s.index('\n    Rectangle { visible: root.current-page == 1 && root.selected-presentation', a)
block = s[a:b].replace('spacing: 6px;', 'spacing: 4px;').replace('height: 68px;', 'height: 52px;').replace('y: 6px;', 'y: 3px;').replace('y: 32px;', 'y: 25px;').replace('y: 34px;', 'y: 25px;')
s = s[:a] + block + s[b:]
p.write_text(s, encoding='utf-8', newline='\n')
edit('src/app.rs', '    window.set_rule_duration_text(', '    window.set_rule_file_text(if english { "File name / File path" } else { "文件名 / 文件路径" }.into());\n    window.set_rule_duration_text(')

# Product version, not only the archive name.
edit('Cargo.toml', 'version = "1.13.0"', 'version = "1.13.1"')
edit('Cargo.lock', 'name = "flyppttimer"\nversion = "1.13.0"', 'name = "flyppttimer"\nversion = "1.13.1"')
edit('resources/app.manifest', 'assemblyIdentity version="1.8.0.0"', 'assemblyIdentity version="1.13.1.0"')
config = json.loads(Path('docs/rc34-default-config.json').read_text(encoding='utf-8'))
config['Version'] = '1.13.1'
config['Placement']['Visible'] = True
Path('docs/v1131-default-config.json').write_text(json.dumps(config, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')
edit('scripts/build-release.ps1', 'docs\\default-config.json', 'docs\\v1131-default-config.json')
p = Path('build.rs')
s = p.read_text(encoding='utf-8')
a = s.index('    fs::write(')
b = s.index('    .expect("write Windows resource script");', a)
s = s[:a] + '''    let version = env::var("CARGO_PKG_VERSION").expect("package version");
    let numeric_version = format!("{},0", version.replace('.', ","));
    let resource = format!(r#"1 ICON "app.ico"
1 24 "app.manifest"
1 VERSIONINFO
FILEVERSION {numeric_version}
PRODUCTVERSION {numeric_version}
FILEFLAGSMASK 0x3fL
FILEFLAGS 0x0L
FILEOS 0x40004L
FILETYPE 0x1L
FILESUBTYPE 0x0L
BEGIN
  BLOCK "StringFileInfo"
  BEGIN
    BLOCK "040904b0"
    BEGIN
      VALUE "FileDescription", "FlyPPTTimer"
      VALUE "FileVersion", "{version}"
      VALUE "InternalName", "FlyPPTTimer"
      VALUE "OriginalFilename", "FlyPPTTimer.exe"
      VALUE "ProductName", "FlyPPTTimer"
      VALUE "ProductVersion", "{version}"
    END
  END
  BLOCK "VarFileInfo"
  BEGIN
    VALUE "Translation", 0x0409, 1200
  END
END
"#);
    fs::write(output.join("FlyPPTTimer.rc"), resource)
''' + s[b:]
p.write_text(s, encoding='utf-8', newline='\n')

scope = '''# FlyPPTTimer v1.13.1

Base: user-accepted RC3.4 ea123cda7f2e73778c2c1a3a5b04839fbd3afae4.

- Mobile list: horizontal swipes switch pages; native vertical list scrolling hands off to the outer page at either boundary, including short lists. Long-press reorder remains available.
- Percentage sliders: hover-wheel adjusts by one percentage point; adjacent editable numbers retain the existing ranges and precision. Valid input previews immediately; Enter/focus loss normalizes input. Apply persists and Cancel discards.
- PC Remote: File name / File path, Duration and Mode headings share the row column widths. Row height is 52 DIP, gap 4 DIP; file paths and enabled status remain.
- Settings after a language restart: OK or Cancel closes Settings, not the timer/tray/Remote service. The obsolete exit-on-settings-close mode has been removed.
- Every process startup shows the timer, even when the previous session saved Visible=false. Hiding remains available for the current session.
- Package, application, Remote API, manifest and Windows file/product versions are 1.13.1, without an RC/test suffix.

No dependency upgrade, main merge, tag or public GitHub Release. Previous accepted features remain unchanged. CI logs and BUILD.txt identify the validated product source. Physical Windows/phone interaction confirmation remains separate from automated checks.
'''
Path('docs/v1/V1131_DELIVERY.md').write_text(scope, encoding='utf-8', newline='\n')
for file in ['HANDOFF.md', 'CODEX_TASK.md']:
    Path('docs/v1', file).write_text('# FlyPPTTimer v1.13.1\n\nDirect ChatGPT implementation; Codex remains paused. Current scope: V1131_DELIVERY.md. BUILD.txt records the packaged product source. Continue from the product commit, not an older RC helper branch. Main unchanged; no public Release/tag requested.\n', encoding='utf-8', newline='\n')
p = Path('docs/v1/APPROVED_PRODUCT_DEVIATIONS.md')
p.write_text(p.read_text(encoding='utf-8') + '\n## 10. v1.13.1 explicit user feedback (2026-09-13)\n\n' + scope.split('\n\n', 2)[2], encoding='utf-8', newline='\n')
p = Path('docs/v1/CODEX_RESULT.md')
p.write_text(p.read_text(encoding='utf-8') + '\n\n# v1.13.1 direct implementation\n\nUser accepted RC3.4. Implemented V1131_DELIVERY.md: list boundary handoff and shared page gestures, wheel and numeric percentage entry, compact aligned Remote columns, ordinary Settings window lifetime, startup visibility reset and version 1.13.1. Exact validation and product source are recorded in CI logs and BUILD.txt.\n', encoding='utf-8', newline='\n')
Path('docs/v1/V1131_USAGE.zh-CN.md').write_text('''# FlyPPTTimer v1.13.1

退出旧版后解压运行 FlyPPTTimer.exe。需要保留设置时，复制旧版 FlyPPTTimer.config.json 到新目录；不要覆盖仍在运行的程序。

- 手机列表横滑切换计时/演示页，竖滑先滚列表、到边界后继续滚整页；长按仍可排序。
- 百分比滑块悬停滚轮每格增减 1%，右侧可直接输入数值，回车或点击别处结束输入。应用后保存，取消可放弃预览。
- Remote 列表表头为文件名/文件路径、时长、模式，行高已收紧。
- 语言重启后点击设置的确定或取消只关闭设置，计时器与遥控继续运行。
- 每次启动都显示计时器，隐藏仅对本次运行生效。
''', encoding='utf-8', newline='\n')
print('v1.13.1 source changes applied')
