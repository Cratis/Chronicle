#!/usr/bin/env python3
"""Keep SQL Server fixtures, local compose and CI pre-pulls on one explicit CU."""
from pathlib import Path
import re
import subprocess

CONSTANT = "Integration/Client/SqlServerContainerImage.cs"
WORKFLOW = ".github/workflows/integration.yml"
EXCLUDED = {".github/scripts/sql-server-image-pin.py", ".github/scripts/test-sql-server-image-pin.py"}
IMAGE = re.compile(r'''mcr\.microsoft\.com/mssql/server:[^\s"'\)]+''')


def pinned_image(root):
    """Read the single source of truth."""
    return re.search(r'const string Name = "([^"]+)"', (root / CONSTANT).read_text()).group(1)


def validate_pin(image):
    """Require an explicit CU from SQL Server 2025 or newer."""
    match = re.fullmatch(r"mcr\.microsoft\.com/mssql/server:(\d{4})-CU(\d+)-ubuntu-\d{2}\.\d{2}", image)
    return match is not None and int(match.group(1)) >= 2025


def references(root):
    """Return tracked image literals with their source locations."""
    paths = subprocess.check_output(["git", "-C", root, "ls-files", "-z"]).decode().split("\0")
    for path in paths:
        if not path or path.startswith("Documentation/") or path in EXCLUDED or not (root / path).is_file():
            continue
        for line, text in enumerate((root / path).read_text(errors="replace").splitlines(), 1):
            for match in IMAGE.finditer(text):
                yield path, line, match.group(0)


def violations(root):
    """Return diagnostics for invalid or inconsistent image references."""
    image = pinned_image(root)
    errors = []
    if not validate_pin(image):
        errors.append((CONSTANT, 1, "SQL Server image must pin an explicit CU from 2025 or newer"))
    pulls = 0
    for path, line, reference in references(root):
        if reference != image:
            errors.append((path, line, f"SQL Server image does not match the pin: {reference}"))
        if path.endswith(".cs") and path != CONSTANT:
            errors.append((path, line, "Use the SqlServerContainerImage.Name constant instead of a literal"))
        if path == WORKFLOW and reference == image:
            pulls += 1
    if pulls < 2:
        errors.append((WORKFLOW, 1, "Expected two pinned SQL Server pre-pulls (client and clustering)"))
    return errors


def main():
    """Emit GitHub Actions annotations and fail on drift."""
    errors = violations(Path(__file__).resolve().parents[2])
    for path, line, message in errors:
        print(f"::error file={path},line={line}::{message}")
    return int(bool(errors))


if __name__ == "__main__":
    raise SystemExit(main())
