# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Generate package-local Python messages and gRPC stubs from Chronicle proto files."""

from __future__ import annotations

import argparse
import shutil
import tempfile
from importlib import import_module
from importlib.resources import files
from pathlib import Path

PACKAGE_ROOT = Path(__file__).parent / "src" / "cratis_chronicle_contracts"
PACKAGE_NAME = PACKAGE_ROOT.name
GENERATED_SUFFIXES = ("_pb2.py", "_pb2.pyi", "_pb2_grpc.py")


def _arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--proto-root", type=Path, required=True)
    return parser.parse_args()


def _clean_generated_files() -> None:
    for path in PACKAGE_ROOT.rglob("*"):
        if path.is_file() and path.name.endswith(GENERATED_SUFFIXES):
            path.unlink()

    generated_directory = PACKAGE_ROOT / "protobuf_net"
    if generated_directory.exists():
        shutil.rmtree(generated_directory)


def _prepare_proto_tree(source: Path, destination: Path) -> list[Path]:
    # Proto files are rooted under a directory named after the installed package so that protoc computes
    # fully-qualified, already-correct imports (e.g. `from cratis_chronicle_contracts import eventtypes_pb2`)
    # instead of the bare top-level imports it would otherwise emit for messages defined in other files.
    proto_files: list[Path] = []
    package_directory = destination / PACKAGE_NAME
    for source_file in sorted(source.rglob("*.proto")):
        relative_path = source_file.relative_to(source)
        normalized_path = Path(*("protobuf_net" if part == "protobuf-net" else part for part in relative_path.parts))
        destination_file = package_directory / normalized_path
        destination_file.parent.mkdir(parents=True, exist_ok=True)
        contents = source_file.read_text().replace(
            '"protobuf-net/bcl.proto"', f'"{PACKAGE_NAME}/protobuf_net/bcl.proto"'
        )
        destination_file.write_text(contents)
        proto_files.append(Path(PACKAGE_NAME) / normalized_path)
    return proto_files


def generate(proto_root: Path) -> None:
    proto_root = proto_root.resolve()
    if not proto_root.is_dir():
        raise SystemExit(f"Proto root does not exist: {proto_root}")

    _clean_generated_files()
    PACKAGE_ROOT.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory() as temporary_directory:
        normalized_root = Path(temporary_directory)
        proto_files = _prepare_proto_tree(proto_root, normalized_root)
        grpc_include_root = files("grpc_tools").joinpath("_proto")
        arguments = [
            "grpc_tools.protoc",
            f"-I{normalized_root}",
            f"-I{grpc_include_root}",
            f"--python_out={PACKAGE_ROOT.parent}",
            f"--pyi_out={PACKAGE_ROOT.parent}",
            f"--grpc_python_out={PACKAGE_ROOT.parent}",
            *(str(path) for path in proto_files),
        ]
        protoc = import_module("grpc_tools.protoc")
        result = protoc.main(arguments)
        if result != 0:
            raise SystemExit(result)

    nested_package = PACKAGE_ROOT / "protobuf_net"
    nested_package.mkdir(exist_ok=True)
    (nested_package / "__init__.py").write_text(
        "# Copyright (c) Cratis. All rights reserved.\n"
        "# Licensed under the MIT license. See LICENSE file in the project root for full license information.\n"
    )


if __name__ == "__main__":
    generate(_arguments().proto_root)
