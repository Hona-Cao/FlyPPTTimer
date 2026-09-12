from pathlib import Path

# Keep each correction narrow and fail closed when its source context changes.
def replace(path, old, new):
    p = Path(path)
    content = p.read_text(encoding='utf-8')
    assert content.count(old) == 1, (path, 'source context changed')
    p.write_bytes(content.replace(old, new).encode('utf-8'))

replace('src/FlyPPTTimer/Web/app.js',
    '''text(row.querySelector('[data-presentation-button]'),wt(item.isActive?"\u5f53\u524d":item.isOpen?"\u5207\u6362":"\u6253\u5f00"));''',
    '''text(row.querySelector('[data-presentation-button]'),effectiveLanguage==='en'&&item.isActive?'Active':wt(item.isActive?"\u5f53\u524d":item.isOpen?"\u5207\u6362":"\u6253\u5f00"));''')
replace('src/desktop.rs', 'GetMessageW, HMENU, IDI_APPLICATION,', 'GetMessageW, HMENU,')
replace('src/desktop.rs', 'LR_DEFAULTCOLOR, LoadIconW, MB_ICONWARNING,', 'LR_DEFAULTCOLOR, MB_ICONWARNING,')
replace('src/desktop.rs',
'''    })();
    if let Some(icon) = custom {
        *TRAY_ICON.get_or_init(Default::default).lock().unwrap() = icon as isize;
        icon
    } else {
        unsafe { LoadIconW(null_mut(), IDI_APPLICATION) }
    }
}''',
'''    })().or_else(|| {
        // If the ICO entry cannot be decoded, use the embedded product resource,
        // not the stock Windows application icon. No LR_SHARED: we own this copy.
        use windows_sys::Win32::UI::WindowsAndMessaging::{IMAGE_ICON, LoadImageW};
        let icon = unsafe {
            LoadImageW(GetModuleHandleW(null()), std::ptr::without_provenance::<u16>(1),
                IMAGE_ICON, 16, 16, LR_DEFAULTCOLOR)
        };
        (!icon.is_null()).then_some(icon)
    });
    if let Some(icon) = custom {
        *TRAY_ICON.get_or_init(Default::default).lock().unwrap() = icon as isize;
        icon
    } else {
        crate::log::error("Unable to load either embedded product icon representation");
        null_mut()
    }
}''')
p = Path('src/FlyPPTTimer/Web/app.css')
p.write_bytes((p.read_text(encoding='utf-8') + '\n@media (max-width:340px) { html[lang="en"] .presentation-actions button { font-size:11px; } }\n').encode('utf-8'))

# A direct layout child of ScrollView is assigned viewport geometry by Slint.
# Reserve the gutter as layout padding, not a width that the viewport overwrites.
for view, prior in [('settings-scroll', 36), ('connection-scroll', 36), ('rules-scroll', 18)]:
    replace('ui/app-window.slint',
        f'width: {view}.visible-width - {prior}px;',
        f'width: {view}.visible-width;\n                    padding-left: 18px;\n                    padding-right: 36px;')
replace('ui/app-window.slint',
    '''for item[index] in root.items: FieldRow {
                        width: parent.width;
                        item: item;''',
    '''for item[index] in root.items: FieldRow {
                        item: item;''')
print('RC3.3 rendered-gutter, narrow-label and product-icon refinements applied')
