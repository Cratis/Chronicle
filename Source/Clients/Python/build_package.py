# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Build the generated contracts package after validating generation output."""

from __future__ import annotations

import shutil
import subprocess
import sys
import tarfile
import zipfile
from pathlib import Path

ROOT = Path(__file__).parent
PACKAGE_ROOT = ROOT / "src" / "cratis_chronicle_contracts"


def main() -> None:
    messages = list(PACKAGE_ROOT.glob("*_pb2.py"))
    stubs = list(PACKAGE_ROOT.glob("*_pb2_grpc.py"))
    if not messages or not stubs:
        raise SystemExit("Generated messages and gRPC stubs are required. Run generate.py first.")

    shutil.rmtree(ROOT / "dist", ignore_errors=True)
    subprocess.run([sys.executable, "-m", "build"], cwd=ROOT, check=True)
    distributions = [*sorted((ROOT / "dist").glob("*.whl")), *sorted((ROOT / "dist").glob("*.tar.gz"))]
    if len(distributions) != 2:
        raise SystemExit(f"Expected one wheel and one source distribution, found {len(distributions)}")

    wheel = next(path for path in distributions if path.suffix == ".whl")
    source_distribution = next(path for path in distributions if path.name.endswith(".tar.gz"))
    required_generated_paths = (
        "cratis_chronicle_contracts/eventtypes_pb2.py",
        "cratis_chronicle_contracts/eventtypes_pb2_grpc.py",
        "cratis_chronicle_contracts/protobuf_net/bcl_pb2.py",
    )
    with zipfile.ZipFile(wheel) as archive:
        wheel_paths = set(archive.namelist())
    missing_from_wheel = [path for path in required_generated_paths if path not in wheel_paths]
    if missing_from_wheel:
        raise SystemExit(f"Generated contracts missing from wheel: {missing_from_wheel}")

    with tarfile.open(source_distribution, "r:gz") as archive:
        source_paths = tuple(archive.getnames())
    missing_from_source = [
        path
        for path in required_generated_paths
        if not any(candidate.endswith(f"/src/{path}") for candidate in source_paths)
    ]
    if missing_from_source:
        raise SystemExit(f"Generated contracts missing from source distribution: {missing_from_source}")

    subprocess.run([sys.executable, "-m", "twine", "check", *(str(path) for path in distributions)], check=True)


if __name__ == "__main__":
    main()
