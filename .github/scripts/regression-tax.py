# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Publishes the regression-tax dashboard for a calendar month, verdicts first.

`regression_tax_metrics.py` measures; this states what the measurements mean. Every number is
printed beside the target it is judged against - a reader should see PASS or FAIL before a raw
percentage - and beside the hand-measured series the reliability program was argued from, so a
definition that drifts away from that analysis shows up as a widening delta instead of quietly
becoming the new truth.

The targets come from the reliability program plan (section 5, task 2.4): volume flat or up,
fix-share below 10%, patch share below 40%, and reverts trending toward the pre-2026 baseline of
roughly one a month. The plan sets no threshold for the regression-language issue rate, so this
reports its direction rather than inventing a verdict for it.

A source that could not be read is written into the dashboard as its reason and exits 1, so the
scheduled run goes red rather than committing a fabricated zero. Run it by hand for any month:

    python3 .github/scripts/regression-tax.py --month 2026-07
    python3 .github/scripts/regression-tax.py --output    # writes Metrics/regression-tax.md
"""

import argparse
import datetime
import statistics
import sys

from regression_tax_metrics import (BASELINE, Unmeasured, determine_repository, measure, months_between,
                                    read_baseline, read_commits, read_history, read_issues, read_releases)

DEFAULT_OUTPUT = "Metrics/regression-tax.md"
FIRST_MONTH = "2025-09"

FIX_SHARE_TARGET = 10.0
PATCH_SHARE_TARGET = 40.0
REVERT_TARGET = 1
VOLUME_TOLERANCE = 0.9
PERCENTAGES = {"Fix/revert commit share", "Patch share of releases", "Regression-language issue rate"}


def preceding(series, month, key, count=3):
    """Returns the measured values of `key` for the months immediately before `month`."""
    earlier = [_ for _ in sorted(series) if _ < month][-count:]
    return [series[_][key] for _ in earlier if not isinstance(series[_][key], Unmeasured)]


def compare(value, reference, passes):
    """Returns a verdict, refusing to judge when either the value or the thing to judge it by is missing."""
    if isinstance(value, Unmeasured):
        return "n/a"
    if reference is None or reference == []:
        return "n/a - no reference yet"
    return "PASS" if passes(value) else "FAIL"


def reverts_verdict(count, recent, earlier):
    """Returns a verdict honoring "trending toward" - above target but falling is WATCH, not FAIL."""
    if isinstance(count, Unmeasured):
        return "n/a"
    if count <= REVERT_TARGET:
        return "PASS"
    if recent and earlier and statistics.mean(recent) < statistics.mean(earlier):
        return "WATCH - above target but trending down"
    return "FAIL"


def trend(value, reference):
    """Returns the direction of a metric the plan sets no threshold for, rather than a false verdict."""
    if isinstance(value, Unmeasured):
        return "n/a"
    if not reference:
        return "n/a - no reference yet"

    mean = statistics.mean(reference)
    direction = "RISING" if value > mean else "FALLING" if value < mean else "FLAT"
    return f"{direction} on a 12-month mean of {mean:.1f}%"


def verdicts(series, month, partial):
    """Returns (metric, value, target, verdict) for the reported month."""
    now = series[month]
    volume = preceding(series, month, "commits")
    median = statistics.median(volume) if volume else None
    language = preceding(series, month, "regression_share", 12)
    earlier = [series[_]["reverts"] for _ in sorted(series) if _ < month][-6:-3]

    return [
        ("Commit volume", now["commits"], f"flat or up on the 3-month median ({format_number(median)})",
         "n/a - partial month" if partial else compare(now["commits"], median,
                                                       lambda value: value >= VOLUME_TOLERANCE * median)),
        ("Fix/revert commit share", now["fix_share"], f"below {FIX_SHARE_TARGET:.0f}%",
         compare(now["fix_share"], FIX_SHARE_TARGET, lambda value: value < FIX_SHARE_TARGET)),
        ("Patch share of releases", now["patch_share"], f"below {PATCH_SHARE_TARGET:.0f}%",
         compare(now["patch_share"], PATCH_SHARE_TARGET, lambda value: value < PATCH_SHARE_TARGET)),
        ("Reverts", now["reverts"], f"{REVERT_TARGET}/month (the pre-2026 baseline)",
         reverts_verdict(now["reverts"], preceding(series, month, "reverts"), earlier)),
        ("Regression-language issue rate", now["regression_share"], "no threshold in the plan - watch the trend",
         trend(now["regression_share"], language)),
    ]


def format_number(value, suffix="", terse=False):
    """Renders a value for the dashboard, never turning a missing measurement into a number."""
    if isinstance(value, Unmeasured):
        return "n/a" if terse else f"n/a - {value.reason}"
    if value is None:
        return "n/a"
    return f"{value:.1f}{suffix}" if isinstance(value, float) else f"{value}{suffix}"


def render_verdicts(series, month, partial):
    """Returns the headline table, where a reader meets the verdict before the raw number."""
    lines = [f"## {month}" + (" (partial month)" if partial else ""), "",
             "| Metric | Value | Target | Verdict |", "|---|---|---|---|"]
    for name, value, target, verdict in verdicts(series, month, partial):
        lines.append(f"| {name} | {format_number(value, '%' if name in PERCENTAGES else '')} | {target} | {verdict} |")
    return lines


def render_row(month, row, published):
    """Returns one month of the series, beside the published fix-share and the gap to it."""
    measured = not isinstance(row["fix_share"], Unmeasured)
    delta = f"{row['fix_share'] - published:+.1f}" if published is not None and measured else "-"
    return (f"| {month} | {format_number(row['commits'], terse=True)} "
            f"| {format_number(row['fix_share'], '%', terse=True)} "
            f"| {format_number(published, '%') if published is not None else '-'} | {delta} "
            f"| {format_number(row['reverts'], terse=True)} | {format_number(row['releases'], terse=True)} "
            f"| {format_number(row['patch_share'], '%', terse=True)} "
            f"| {format_number(row['issues'], terse=True)} "
            f"| {format_number(row['regression_share'], '%', terse=True)} |")


def render_series(series, baseline):
    """Returns the whole series beside the published baseline, so definition drift stays visible."""
    published = {} if isinstance(baseline, Unmeasured) else baseline
    lines = [
        "## Series", "",
        "`Published` is the hand-measured fix-share from the year-long analysis, kept in",
        f"`{BASELINE}`. A widening `Delta` means this script and that analysis have stopped",
        "measuring the same thing - which matters more than any single month's number.", "",
        "| Month | Commits | Fix/revert share | Published | Delta | Reverts | Releases "
        "| Patch share | Issues | Regression language |",
        "|---|---|---|---|---|---|---|---|---|---|",
    ]
    lines += [render_row(_, series[_], published.get(_)) for _ in sorted(series)]
    return lines + [
        "",
        "`Regression language` counts every issue in the repository, including the ones this program",
        "files *about* regressions - read a spike next to `Issues` before reading it as user pain.",
    ]


def render_sources(sources):
    """Returns the reason behind every `n/a`, so an unread source can never pass for a measurement."""
    unread = {name: source for name, source in sources.items() if isinstance(source, Unmeasured)}
    if not unread:
        return []

    lines = ["## Unread sources", "",
             "Every `n/a` above comes from one of these - none of them is a measured zero.", ""]
    for name, source in sorted(unread.items()):
        lines.append(f"- **{name}** - {source.reason}." + ("" if source.degraded else " Requested, not a failure."))
    return lines


def render(series, month, partial, baseline, sources, repository, generated):
    """Returns the dashboard markdown: the reported month's verdict, then the whole series."""
    body = [
        "# Regression tax", "",
        "<!-- Generated by .github/scripts/regression-tax.py - do not edit by hand. -->", "",
        f"Generated {generated} from the git history of this clone and the issue search for",
        f"`{repository or 'an undetermined repository'}`. The metric definitions live in",
        "`.github/scripts/regression_tax_metrics.py`; the targets come from",
        "the reliability program plan (section 5, task 2.4).", "",
    ]
    body += render_verdicts(series, month, partial) + [""] + render_series(series, baseline)
    body += [""] + render_sources(sources)
    while body and not body[-1]:
        body.pop()
    return "\n".join(body) + "\n"


def parse_arguments():
    """Returns the command line, which defaults to reporting the current month to standard output."""
    parser = argparse.ArgumentParser(description="Publish the regression-tax dashboard for a calendar month.")
    parser.add_argument("--root", default=".", help="repository to measure (default: the current directory)")
    parser.add_argument("--month", help="month to report on as YYYY-MM (default: the current UTC month)")
    parser.add_argument("--since", default=FIRST_MONTH, help=f"first month of the series (default: {FIRST_MONTH})")
    parser.add_argument("--repo", help="owner/name to search issues in (default: GITHUB_REPOSITORY or origin)")
    parser.add_argument("--output", nargs="?", const=DEFAULT_OUTPUT,
                        help=f"write the dashboard to a file (default when given no path: {DEFAULT_OUTPUT})")
    parser.add_argument("--no-issues", action="store_true", help="skip the GitHub issue search without degrading")
    return parser.parse_args()


def main():
    """Computes the series, publishes the dashboard, and fails when a source could not be read."""
    arguments = parse_arguments()
    today = datetime.datetime.now(datetime.timezone.utc)
    month = arguments.month or today.strftime("%Y-%m")
    months = months_between(min(arguments.since, month), month)

    truncated = read_history(arguments.root)
    commits = truncated or read_commits(arguments.root)
    releases = truncated or read_releases(arguments.root)
    repository = determine_repository(arguments.root, arguments.repo)
    issues = (Unmeasured("the issue search was skipped with --no-issues", degraded=False)
              if arguments.no_issues else read_issues(repository, months))

    baseline = read_baseline(arguments.root)

    sources = {"Commit history": commits, "Release tags": releases, "GitHub issues": issues,
               "Published fix-share baseline": baseline}
    series = {_: measure(_, commits, releases, issues) for _ in months}
    dashboard = render(series, month, month == today.strftime("%Y-%m"), baseline,
                       sources, repository, today.strftime("%Y-%m-%d %H:%M UTC"))

    if arguments.output:
        with open(f"{arguments.root}/{arguments.output}", "w", encoding="utf-8") as file:
            file.write(dashboard)
        print(f"Wrote {arguments.output} for {month}")
    else:
        print(dashboard, end="")

    degraded = {name: _ for name, _ in sources.items() if isinstance(_, Unmeasured) and _.degraded}
    for name, source in sorted(degraded.items()):
        print(f"::error::{name} could not be read - {source.reason}. The dashboard says so instead of "
              f"reporting zero; rerun once the source is back.")
    return 1 if degraded else 0


if __name__ == "__main__":
    sys.exit(main())
