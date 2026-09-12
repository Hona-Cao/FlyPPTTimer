"""Keep the percentage editor and slider on one current value."""
from pathlib import Path
p = Path('ui/app-window.slint')
s = p.read_text(encoding='utf-8')
a = s.index('    edited(text) => {', s.index('component PercentageInput'))
b = s.index('\ncomponent RemoteButton', a)
s = s[:a] + '''    edited(text) => {
        if text.is-float() {
            root.value-edited(clamp(round(text.to-float() / root.step) * root.step, root.minimum, root.maximum));
        }
    }
    accepted(text) => { self.clear-focus(); }
    changed has-focus => {
        // Valid input is already applied; never replay old text over a slider change.
        if !self.has-focus { self.text = "" + root.value; }
    }
}
''' + s[b:]
s = s.replace('                if delta == 0px { return reject; }\n', '                if delta == 0px { return reject; }\n                percent-input.clear-focus();\n', 1)
s = s.replace('        PercentageInput {', '        percent-input := PercentageInput {', 1)
p.write_text(s, encoding='utf-8', newline='\n')
