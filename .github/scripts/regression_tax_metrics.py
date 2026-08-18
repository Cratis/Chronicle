# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Measures the regression tax for a calendar month. The definitions live here and nowhere else.

The reliability program was argued from four numbers taken by hand over a year. A number measured
once is an anecdote: without a machine that reproduces it every month the program has no feedback
loop and quietly becomes folklore. `regression-tax.py` publishes what this module measures.

Every metric buckets by UTC calendar month, so the answer does not depend on the machine's clock:

  Commit volume            non-merge commits, by committer date. Merge commits are excluded because
                           a merge is not a unit of work and its generated subject can never match
                           a fix stem, so counting them only dilutes the share.
  Fix/revert share         the share of those whose subject begins with a fix or revert stem. The
                           stem is what matters: 1,761 subjects in this repository begin "Fixing",
                           so reading the plan's "starts with Fix" literally reproduces about a
                           third of the published series. See Metrics/fix-share-baseline.txt.
  Revert count             commits whose subject begins with a revert stem. The plan's shorthand
                           `git rev-list -i --grep=revert` searches the whole message and counts
                           every commit that merely mentions reverting; this counts the ones that
                           are a revert.
  Patch share of releases  semver tags created in the month whose patch component is non-zero.
  Regression language      issues opened in the month whose title or body says regression, broke,
                           no longer works, silently, or stopped working.

A source that cannot be read returns `Unmeasured` rather than an empty result, because a metric
that silently reads zero is worse than one that fails - an empty commit log and a shallow clone
are indistinguishable from the numbers alone.
"""

import collections
import datetime
import json
import os
import re
import shutil
import subprocess

FIX_STEM = re.compile(r'^\s*(fix|revert|restore|unbreak|hotfix)', re.IGNORECASE)
REVERT_STEM = re.compile(r'^\s*revert', re.IGNORECASE)
REGRESSION_LANGUAGE = re.compile(r'regression|broke|no longer works|silently|stopped working', re.IGNORECASE)
SEMVER_TAG = re.compile(r'^v?(\d+)\.(\d+)\.(\d+)(?:-[0-9A-Za-z.-]+)?$')
REMOTE_REPOSITORY = re.compile(r'[:/]([^/:]+/[^/]+?)(?:\.git)?$')

BASELINE = "Metrics/fix-share-baseline.txt"
ISSUE_PAGE_LIMIT = 1000


class Unmeasured:
    """A metric with no number behind it, carrying the reason so it can never be read as a zero.

    `degraded` separates "the source failed" from "the month genuinely has no denominator" - only
    the former should turn the scheduled run red.
    """

    def __init__(self, reason, degraded=True):
        self.reason = reason
        self.degraded = degraded


def git(root, *arguments):
    """Runs a git command in the repository and returns its stdout."""
    return subprocess.run(["git", "-C", root, *arguments], capture_output=True, text=True, check=True).stdout


def month_of(timestamp):
    """Returns the UTC calendar month of a unix timestamp, so a run is not timezone-dependent."""
    return datetime.datetime.fromtimestamp(int(timestamp), datetime.timezone.utc).strftime("%Y-%m")


def months_between(first, last):
    """Returns every calendar month from `first` to `last` inclusive."""
    year, month = (int(_) for _ in first.split("-"))
    result = []
    while f"{year:04d}-{month:02d}" <= last:
        result.append(f"{year:04d}-{month:02d}")
        year, month = (year + 1, 1) if month == 12 else (year, month + 1)
    return result


def read_history(root):
    """Returns why the clone cannot answer for history, or None when it can."""
    try:
        if git(root, "rev-parse", "--is-shallow-repository").strip() == "true":
            return Unmeasured("the clone is shallow, so check it out with fetch-depth 0 to measure history")
    except (subprocess.CalledProcessError, FileNotFoundError):
        return Unmeasured("git could not read this directory as a repository")
    return None


def read_commits(root):
    """Returns the subjects of every non-merge commit reachable from HEAD, keyed by month."""
    by_month = collections.defaultdict(list)
    for line in git(root, "log", "--no-merges", "--format=%ct%x1f%s").splitlines():
        stamp, separator, subject = line.partition("\x1f")
        if separator:
            by_month[month_of(stamp)].append(subject)
    return by_month


def read_releases(root):
    """Returns the semver component triples of every release tag, keyed by month."""
    by_month = collections.defaultdict(list)
    for line in git(root, "for-each-ref", "--format=%(refname:short)%09%(creatordate:unix)", "refs/tags").splitlines():
        name, separator, stamp = line.partition("\t")
        match = SEMVER_TAG.match(name) if separator else None
        if match:
            by_month[month_of(stamp)].append(tuple(int(match.group(_)) for _ in (1, 2, 3)))

    if not by_month:
        return Unmeasured("this clone holds no release tags - fetch tags to measure releases")
    return by_month


def read_issues(repository, months):
    """Returns the title and body of every issue opened in each month, via the GitHub CLI."""
    if not repository:
        return Unmeasured("the repository could not be determined - pass --repo")
    if not shutil.which("gh"):
        return Unmeasured("the gh CLI is not installed")

    by_month = {}
    for month in months:
        start = datetime.date(*(int(_) for _ in month.split("-")), 1)
        end = (start + datetime.timedelta(days=31)).replace(day=1) - datetime.timedelta(days=1)
        finished = subprocess.run(
            ["gh", "issue", "list", "--repo", repository, "--state", "all", "--limit", str(ISSUE_PAGE_LIMIT),
             "--search", f"created:{start}..{end}", "--json", "title,body"], capture_output=True, text=True)
        if finished.returncode != 0:
            detail = [_.strip() for _ in finished.stderr.splitlines() if _.strip()]
            return Unmeasured(f"the GitHub issue search failed - {detail[0].rstrip('.') if detail else 'no detail given'}")

        issues = json.loads(finished.stdout)
        if len(issues) >= ISSUE_PAGE_LIMIT:
            return Unmeasured(f"the issue search for {month} hit the {ISSUE_PAGE_LIMIT}-result cap")
        by_month[month] = [f"{_['title']}\n{_['body'] or ''}" for _ in issues]
    return by_month


def read_baseline(root):
    """Returns the published fix-share per month from the committed hand-measured series.

    A missing or empty file is a failure, not an empty comparison. The delta against this series is
    the one number that catches the definitions drifting apart, and it fails silently by construction
    - drop the file and every `Published` cell simply renders blank, which reads like a clean run.
    """
    path = os.path.join(root, BASELINE)
    if not os.path.isfile(path):
        return Unmeasured(f"{BASELINE} is missing, so no month can be compared with the published analysis")

    published = {}
    with open(path, encoding="utf-8") as file:
        for line in file:
            entry = line.split("#")[0].strip()
            if entry:
                month, percent = entry.split()
                published[month] = float(percent)

    if not published:
        return Unmeasured(f"{BASELINE} lists no months, so there is nothing to compare a measurement with")
    return published


def determine_repository(root, requested):
    """Returns the owner/name to search issues in, preferring an explicit value over discovery."""
    if requested:
        return requested
    if os.environ.get("GITHUB_REPOSITORY"):
        return os.environ["GITHUB_REPOSITORY"]

    try:
        origin = git(root, "remote", "get-url", "origin").strip()
    except (subprocess.CalledProcessError, FileNotFoundError):
        return None

    match = REMOTE_REPOSITORY.search(origin) if "github.com" in origin else None
    return match.group(1) if match else None


def share(matching, total):
    """Returns a percentage, or an unmeasured cell when the month has no denominator at all."""
    if not total:
        return Unmeasured("no observations in the month", degraded=False)
    return 100.0 * matching / total


def measure(month, commits, releases, issues):
    """Returns every metric for one month, each either a number or the reason there is none."""
    subjects = commits if isinstance(commits, Unmeasured) else commits.get(month, [])
    tags = releases if isinstance(releases, Unmeasured) else releases.get(month, [])
    texts = issues if isinstance(issues, Unmeasured) else issues.get(month, [])

    return {
        "commits": subjects if isinstance(subjects, Unmeasured) else len(subjects),
        "fix_share": subjects if isinstance(subjects, Unmeasured)
        else share(sum(1 for _ in subjects if FIX_STEM.match(_)), len(subjects)),
        "reverts": subjects if isinstance(subjects, Unmeasured)
        else sum(1 for _ in subjects if REVERT_STEM.match(_)),
        "releases": tags if isinstance(tags, Unmeasured) else len(tags),
        "patch_share": tags if isinstance(tags, Unmeasured)
        else share(sum(1 for _ in tags if _[2]), len(tags)),
        "issues": texts if isinstance(texts, Unmeasured) else len(texts),
        "regression_share": texts if isinstance(texts, Unmeasured)
        else share(sum(1 for _ in texts if REGRESSION_LANGUAGE.search(_)), len(texts)),
    }
