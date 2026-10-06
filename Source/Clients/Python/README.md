# Chronicle Python contracts

This project packages the non-idiomatic Python gRPC messages and stubs generated from Chronicle's canonical
`.proto` files. The package is intended for the idiomatic
[`Cratis/Chronicle.Python`](https://github.com/Cratis/Chronicle.Python) client.

Generated modules are build artifacts and must not be edited. Change the Chronicle C# contracts or proto
generator, regenerate the schemas, and build this package again.

## Build locally

From this directory:

```shell
python -m venv .venv
source .venv/bin/activate
python -m pip install -e ".[dev]"
python generate.py --proto-root ../../Kernel/Protobuf
pytest
python build_package.py
```

`build_package.py` refuses to build when generated messages or gRPC stubs are absent.

## Distribution

This package is not published to PyPI. While publication is unconfigured, a wheel and sdist built from an exact
release tag are attached to that tag's GitHub release by the *Attach Python Contract Release Assets* workflow, with
`SHA256SUMS` and `provenance.json`. Pin the wheel by URL and hash. See
`Documentation/contributing/clients/python-contract-assets.md`.

This package exposes the generated wire surface. It does not establish support, compatibility, or feature parity
for an idiomatic Python client.
