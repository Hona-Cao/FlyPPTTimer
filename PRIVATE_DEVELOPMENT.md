# Private development repository policy

This repository is intended to be the author's primary private development repository for FlyPPTTimer and the canonical cloud copy used to continue development across computers.

## Repository status

- The repository should be kept **Private** on GitHub.
- Source code, development notes, build scripts, test material, unreleased assets, and internal documentation may be stored here.
- Do not mirror the full repository or proprietary source tree to a public Git host.
- Public distribution, when desired, should contain only official binaries, release notes, user documentation, checksums/signatures, updater metadata, and other deliberately selected public artifacts.

## Licensing boundary

FlyPPTTimer versions up to and including **v1.15.0** were publicly released under the MIT License. Rights already granted under those releases remain valid and are not revoked by making this repository private.

Unless a later release expressly states otherwise, post-v1.15.0 official releases are intended to be closed-source freeware: the application remains free to download and use under the applicable FlyPPTTimer Freeware License, while new proprietary source code is not publicly licensed.

## Historical acknowledgement

old9/ppttimer was an early inspiration during the project's exploratory stage. Later FlyPPTTimer development moved to an independent implementation. This acknowledgement is historical courtesy and does not change the licensing terms of FlyPPTTimer.

## Working across computers

Treat GitHub as the canonical source repository. Before changing machines, commit and push meaningful work. On a new machine, clone the private repository using the author's authenticated GitHub account and continue from the relevant branch.

Do not commit passwords, access tokens, private keys, signing certificates, recovery codes, or machine-specific secrets. Store those in GitHub Actions Secrets, the operating system credential store, or another appropriate secret manager.