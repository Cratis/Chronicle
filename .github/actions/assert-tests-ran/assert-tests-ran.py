# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Fails the job when the TRX files a test step produced report zero executed tests.

A test run that matches nothing is not a failure to `dotnet test` in every configuration, and a
job whose only signal is the exit code therefore reports a broken filter, a project missing from
the solution, or a test host that never started as a pass. Summing the executed counters across
every TRX file the step wrote turns "nothing ran" into a red job with a message that names the
cause.
"""

import glob
import os
import sys
import xml.etree.ElementTree as ET

NAMESPACE = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def executed_in(path):
    """Returns the number of executed tests recorded in a single TRX file."""
    try:
        tree = ET.parse(path)
    except ET.ParseError as error:
        print(f"::warning::Could not parse {path}: {error}")
        return 0

    return sum(int(counters.get("executed") or 0) for counters in tree.iter(f"{NAMESPACE}Counters"))


def main():
    """Sums the executed counters under the given directory and fails when the total is zero."""
    directory = sys.argv[1]
    name = sys.argv[2] if len(sys.argv) > 2 else "The test run"

    files = sorted(glob.glob(os.path.join(directory, "**", "*.trx"), recursive=True))
    executed = sum(executed_in(_) for _ in files)

    print(f"{name}: {executed} executed test(s) across {len(files)} TRX file(s) under {directory}")

    if executed == 0:
        print(
            f"::error::{name} executed 0 tests. A test step that runs nothing can still exit "
            f"successfully, so this is a broken filter, a project missing from the solution, or a "
            f"test host that never started - not a pass. No TRX file under {directory} reported a "
            f"non-zero executed counter."
        )
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
