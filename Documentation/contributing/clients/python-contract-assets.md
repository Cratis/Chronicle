---
title: Python contracts as release assets
description: How the generated Python gRPC contracts are attached to a Chronicle release while PyPI publication is not configured.
---

The Python gRPC contracts (`cratis-chronicle-contracts`) are generated from Chronicle's canonical `.proto` files. They are **not published to PyPI**: trusted publishing has not been configured, and the publish job in `publish-python-client.yml` stays disabled. Until it is, a wheel and source distribution are attached to the GitHub release of the exact tag they were generated from. This is a temporary convention, and it is not a statement that Python is supported or at parity with the other clients.

## What gets attached

For a published release tag such as `v19.31.2`, the release receives four assets:

| Asset | Purpose |
|---|---|
| `cratis_chronicle_contracts-19.31.2-py3-none-any.whl` | The wheel a consumer installs |
| `cratis_chronicle_contracts-19.31.2.tar.gz` | The source distribution |
| `SHA256SUMS` | SHA-256 of the wheel and the source distribution |
| `provenance.json` | Source tag and commit, descriptor hash and the file hashes |

Pin the wheel by URL and hash in the consumer, using the value in `SHA256SUMS`.

## How the assets are produced

The *Attach Python Contract Release Assets* workflow runs by manual dispatch only. It takes an exact stable tag and:

- rejects anything that is not `vMAJOR.MINOR.PATCH`, and requires a published, non-draft, non-prerelease release for that tag;
- checks out the tag itself, so the version is the tag's version and no later source is relabeled;
- regenerates the protos with the real proto generator and fails if they differ from what the tag committed;
- generates the Python modules with `generate.py` (never edited by hand), runs the package tests, builds, and runs `twine check`;
- verifies the wheel version, name and generated contents, and writes the checksum and provenance files;
- installs the wheel into a clean environment on Python 3.10, 3.11, 3.12, 3.13 and 3.14 and exercises messages, gRPC stubs, the `bcl` `Guid`, event sources and the event source type and stream fields on append requests.

The wheel requires Python 3.10 or newer, so it does not install on older interpreters.

## Attaching

A dispatch with `attach` set to `false` (the default) is a dry run: everything is built and tested and nothing is uploaded. Attaching needs `attach` set to `true` and a dispatch from `main`. It uploads only to the release of the same tag, and never overwrites: an existing asset with identical content is skipped, and one with different content fails the run. After uploading it re-reads the release and requires every asset to be attached with the verified content.
