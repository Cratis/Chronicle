# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Fails the job when the TRX files a test step produced report zero executed tests.

A test run that matches nothing is not a failure to `dotnet test` in every configuration, and a
job whose only signal is the exit code therefore reports a broken filter, a project missing from
the solution, or a test host that never started as a pass. Summing the executed counters across
every TRX file the step wrote turns "nothing ran" into a red job with a message that names the
cause.

A job can also run several namespaces at once. An optional comma-separated list of class-name
prefixes then asks for a stronger guard: every prefix must own at least one executed result, so one
member that matches nothing cannot hide behind the others that ran.
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


def executed_class_names_in(path):
    """Returns the class name of every result in a TRX file whose outcome is not NotExecuted."""
    try:
        tree = ET.parse(path)
    except ET.ParseError:
        return []

    class_by_id = {
        test.get("id"): method.get("className") or ""
        for test in tree.iter(f"{NAMESPACE}UnitTest")
        for method in test.iter(f"{NAMESPACE}TestMethod")
    }
    return [
        class_by_id.get(result.get("testId"), "")
        for result in tree.iter(f"{NAMESPACE}UnitTestResult")
        if result.get("outcome") != "NotExecuted"
    ]


def members_without_executed_tests(files, prefixes):
    """Returns the prefixes that no executed result's class name starts with (as prefix + '.')."""
    class_names = [class_name for path in files for class_name in executed_class_names_in(path)]
    return [prefix for prefix in prefixes if not any(class_name.startswith(prefix + ".") for class_name in class_names)]


def main():
    """Sums the executed counters under the given directory and fails when the total is zero."""
    directory = sys.argv[1]
    name = sys.argv[2] if len(sys.argv) > 2 else "The test run"
    prefixes = [_.strip() for _ in sys.argv[3].split(",") if _.strip()] if len(sys.argv) > 3 else []

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

    empty = members_without_executed_tests(files, prefixes)
    if empty:
        print(
            f"::error::{name} executed no test for {', '.join(empty)}. Each member of a batched job must "
            f"run at least one test: a member that matches nothing is a broken filter or a namespace "
            f"missing from the project, and the other members' results must not hide it."
        )
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
