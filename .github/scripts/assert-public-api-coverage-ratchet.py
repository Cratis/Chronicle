# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Fails the build when a public type in a shipped package gains no coverage at all.

Coverage is collected on every run and charted under `Documentation/statistics`, but it gates
nothing, so a public type can ship with no spec touching it and nothing says so. `AppendResult`'s
assertion helpers - `AppendedEventWithResultShouldExtensions`, 36 lines, shipped for consumers to
call - have zero covered lines today.

The predicate is *zero covered lines*, deliberately, not a percentage. A ratchet on percentage
argues about how much coverage is enough, which is a conversation without an end; zero is the one
number that is indefensible, and the one this repository has actual instances of.

**This reads real coverage rather than grepping for type names, and that distinction is the whole
design.** The obvious cheap implementation - does any spec file mention the type's name - reports
that `EventScenarioGivenBuilder`, `EventScenarioWhenBuilder`, `EventSourceGivenBuilder` and
`EventSourceWhenBuilder` are uncovered. All four are at or near 100% line coverage. They are fluent
builders reached by property chaining: `EventScenario.Given` returns one, so every `.Given` in the
suite exercises it without ever writing its name. A name-grep gate would demand specs for code that
is already covered, and the natural way to satisfy it is to mention the type somewhere - which adds
a citation, not a test. A gate that can be satisfied without testing anything is worse than none.

Run with `--report` to print the current zero-covered set in baseline format. That is how the
baseline is seeded and regenerated; it only writes to stdout, so the change still arrives as a
reviewable diff.
"""

import os
import re
import sys
import xml.etree.ElementTree as ElementTree

BASELINE = ".github/public-api-coverage-baseline.txt"
RESULTS = "coverage-results"

# Only assemblies shipped to consumers. An internal project's public surface is public to the
# solution, not to anybody who installs a package, so holding it to this bar would spend the
# ratchet's credibility on types no consumer can call.
SHIPPED_PREFIXES = ("Cratis.Chronicle.Testing",)

# The exported surface, one type per line, produced from the built assemblies. Cobertura reports
# every type it instruments, `internal` ones included - `InMemoryEventCursor` and
# `NoOpEventTypesCacheClient` are both `internal sealed` and both appear in the report under a
# shipped namespace. Filtering on the namespace alone would demand consumer-facing specs for types
# no consumer can reference, so the namespace decides which assembly and this file decides what is
# actually public.
SURFACE = ".github/public-api-surface.txt"

# Cobertura writes a compiler-generated state machine as `Type/<Method>d__12`, and a generic as
# `Type`1`. Both have to collapse onto the declaring type or the same type is counted several
# times under names no baseline could track.
NESTED = re.compile(r"/.*$")
ARITY = re.compile(r"`\d+$")


def declaring_type(name):
    """Returns the outermost non-generic type name for a cobertura class entry."""
    return ARITY.sub("", NESTED.sub("", name))


def is_shipped(name):
    """Returns whether a type belongs to an assembly that ships to consumers."""
    return any(name.startswith(_) for _ in SHIPPED_PREFIXES)


def read_surface(root):
    """Returns the exported type names, or an empty set when the surface file is absent."""
    path = os.path.join(root, SURFACE)
    if not os.path.isfile(path):
        return set()

    try:
        with open(path, encoding="utf-8") as file:
            return {stripped for stripped in (_.split("#")[0].strip() for _ in file) if stripped}
    except OSError as error:
        raise CoverageUnreadable(f"`{SURFACE}` exists but could not be read ({error}).") from error


def coverage_reports(root):
    """Returns every cobertura report under the results directory."""
    results = os.path.join(root, RESULTS)
    if not os.path.isdir(results):
        return []

    found = []
    for directory, _, files in os.walk(results):
        found.extend(os.path.join(directory, _) for _ in files if _.endswith("cobertura.xml"))

    return sorted(found)


def covered_lines(reports):
    """Returns hit and total line counts per declaring type, summed across every report.

    Summing across reports matters: each test project writes its own file, and a type exercised
    only by a sibling project would otherwise read as uncovered in the report that does not touch
    it. The union is the honest measure of what the suite as a whole covers.
    """
    totals = {}
    for report in reports:
        try:
            root = ElementTree.parse(report).getroot()
        except (ElementTree.ParseError, OSError) as error:
            raise CoverageUnreadable(f"`{report}` could not be read as cobertura XML ({error}).") from error

        for element in root.iter("class"):
            name = element.get("name") or ""
            if not is_shipped(name):
                continue

            hits = total = 0
            for line in element.iter("line"):
                total += 1
                try:
                    covered = int(line.get("hits") or 0)
                except ValueError as error:
                    raise CoverageUnreadable(
                        f"`{report}` has a line in `{name}` whose `hits` attribute is "
                        f"`{line.get('hits')}`, which is not a number. Coverage that cannot be parsed "
                        f"is not the same as coverage of zero, so this fails rather than accusing the type."
                    ) from error

                if covered > 0:
                    hits += 1

            previous_hits, previous_total = totals.get(declaring_type(name), (0, 0))
            totals[declaring_type(name)] = (previous_hits + hits, previous_total + total)

    return totals


class CoverageUnreadable(Exception):
    """Raised when coverage or the baseline cannot be read, which is never the same as nothing being covered."""


def zero_covered(totals):
    """Returns the shipped types that have lines but none of them hit."""
    return sorted(name for name, (hits, total) in totals.items() if total > 0 and hits == 0)


def read_baseline(root):
    """Returns the type names the baseline currently tolerates.

    A baseline that exists but cannot be read is an error rather than an empty set: treating it as
    empty would report every baselined type as a new failure at once, burying any real one.
    """
    path = os.path.join(root, BASELINE)
    if not os.path.isfile(path):
        return set()

    try:
        with open(path, encoding="utf-8") as file:
            return {stripped for stripped in (_.split("#")[0].strip() for _ in file) if stripped}
    except OSError as error:
        raise CoverageUnreadable(f"`{BASELINE}` exists but could not be read ({error}).") from error


def report(names):
    """Prints the current zero-covered set in baseline format."""
    for name in names:
        print(name)


def main():
    """Reports every shipped public type that has no covered line and is not already baselined."""
    root = sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1].startswith("-") else "."

    reports = coverage_reports(root)
    if not reports:
        print(
            f"::error::No cobertura report found under `{RESULTS}`. This check reads the coverage the "
            f"specs job already collects; without it the check would pass while measuring nothing, so "
            f"it fails instead. Run the specs with `--collect:\"XPlat Code Coverage\"` first."
        )
        return 1

    try:
        totals = covered_lines(reports)
        allowed = read_baseline(root)
        surface = read_surface(root)
    except CoverageUnreadable as error:
        print(f"::error::{error}")
        return 1

    if not surface:
        print(
            f"::error::`{SURFACE}` is missing or empty, so public and internal types cannot be told "
            f"apart and this check would demand specs for types no consumer can reference. Regenerate "
            f"it with `python3 .github/scripts/report-public-api-surface.py > {SURFACE}`."
        )
        return 1

    # The "measuring nothing" guard is judged on what the report contained, before the surface
    # filter narrows it to public types. Judging it afterwards would report a run that legitimately
    # exercised only `internal` types as a broken measurement, which is a different failure with a
    # misleading message.
    if not totals:
        print(
            f"::error::{len(reports)} cobertura report(s) were read but none contained a type from "
            f"{', '.join(SHIPPED_PREFIXES)}. Either the shipped assemblies were not exercised or the "
            f"prefixes are stale - both mean this check is measuring nothing."
        )
        return 1

    totals = {name: counts for name, counts in totals.items() if name in surface}
    current = zero_covered(totals)

    if "--report" in sys.argv:
        report(current)
        return 0

    print(
        f"{len(totals)} shipped public type(s) measured against {len(surface)} exported; "
        f"{len(current)} with no covered line, {len(allowed)} baselined"
    )

    failed = False
    for name in current:
        if name not in allowed:
            hits, total = totals[name]
            failed = True
            print(
                f"::error::`{name}` is public in a shipped package and no spec covers any of its "
                f"{total} lines. A consumer can call it and nothing verifies it works. Add a spec, or - "
                f"if it is genuinely untestable - add it to {BASELINE} and say in the pull request why."
            )

    for name in sorted(allowed - set(current)):
        print(
            f"::notice::`{name}` now has covered lines - remove it from {BASELINE} so the "
            f"baseline cannot silently drift back up."
        )

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
