# Third-party notices and acknowledgements

FlyPPTTimer includes or depends on third-party software. Those components remain
subject to their own licenses; the FlyPPTTimer project license does not replace
or narrow rights granted by those third-party licenses.

## Runtime and library dependencies

The application uses Rust crates and Windows components listed by the current
build metadata (`Cargo.toml` / `Cargo.lock`). Official Windows packages may also
include Microsoft Visual C++ runtime DLLs. Those Microsoft runtime files are
Microsoft components and are not owned or relicensed by FlyPPTTimer.

When redistributing an official package with written permission, preserve all
third-party notices that accompany that package.

## Inspiration acknowledgement: old9/ppttimer

The earlier project **old9/ppttimer** was an inspiration during the initial
exploration of presentation-timer behavior. FlyPPTTimer later moved to an
independent implementation and does not intentionally reuse old9/ppttimer source
code, icons, images, sound assets, UI, or its AHK build chain.

The acknowledgement is retained as a courtesy and development-history note.
The old9/ppttimer project used the MIT License; that fact does not make current
FlyPPTTimer releases derivatives of that project and does not determine the
license of later FlyPPTTimer releases.

## Historical FlyPPTTimer releases

FlyPPTTimer releases up to and including v1.15.0 were themselves published
under the MIT License. Their historical license remains applicable to those
versions. See `LICENSE` for the licensing boundary governing later original
material and official releases.
