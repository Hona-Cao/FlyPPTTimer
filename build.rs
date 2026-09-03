use std::{env, fs, path::PathBuf, process::Command};

fn main() {
    slint_build::compile("ui/app-window.slint").expect("compile the Slint UI");
    println!("cargo:rerun-if-changed=src/FlyPPTTimer/Assets/app.ico");
    println!("cargo:rerun-if-changed=resources/app.manifest");

    if env::var("CARGO_CFG_TARGET_OS").as_deref() != Ok("windows")
        || env::var("CARGO_CFG_TARGET_ENV").as_deref() != Ok("msvc")
    {
        return;
    }

    let root = PathBuf::from(env::var_os("CARGO_MANIFEST_DIR").expect("manifest directory"));
    let output = PathBuf::from(env::var_os("OUT_DIR").expect("build output directory"));
    let res_file = output.join("FlyPPTTimer.res");
    fs::copy(
        root.join("src/FlyPPTTimer/Assets/app.ico"),
        output.join("app.ico"),
    )
    .expect("copy Windows icon to build directory");
    fs::copy(
        root.join("resources/app.manifest"),
        output.join("app.manifest"),
    )
    .expect("copy Windows manifest to build directory");
    fs::write(
        output.join("FlyPPTTimer.rc"),
        "1 ICON \"app.ico\"\r\n1 24 \"app.manifest\"\r\n",
    )
    .expect("write Windows resource script");

    let compiler = env::var_os("RC").unwrap_or_else(|| "rc.exe".into());
    let status = Command::new(compiler)
        .current_dir(&output)
        .arg("/nologo")
        .args(["/fo", "FlyPPTTimer.res", "FlyPPTTimer.rc"])
        .status()
        .expect("run Windows resource compiler (rc.exe)");
    assert!(status.success(), "Windows resource compiler failed");
    println!("cargo:rustc-link-arg-bins={}", res_file.display());
}
