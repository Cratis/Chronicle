# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Reviewed lane exclusions, never inferred from a zero-execution test result."""

import os
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
WAIVERS = ".github/grain-compatibility-waivers.txt"
UNUSABLE = ".github/rolling-upgrade-unusable-releases.txt"

# Remove the entry when the rolling-upgrade spec is re-enabled; no runner waiver is needed.
EXCLUDED_LANES = {}


def listed_tags(text):
    """The release tags a list file names: the first word of every line that is not a comment."""
    return {line.split()[0] for line in text.splitlines() if line.strip() and not line.lstrip().startswith("#")}


def previous_release(tags, unusable_text):
    """The release the RollingUpgrade lane upgrades from: the newest release tag it can start a cluster from."""
    unusable = {line.split("#")[0].strip() for line in unusable_text.splitlines()}
    usable = [tag for tag in tags if re.fullmatch(r"v\d+\.\d+\.\d+", tag) and tag not in unusable]
    return usable[-1] if usable else None


def waived_release(tags, unusable_text, waivers_text):
    """The previous release when it is waived as unable to share a cluster with this build, otherwise None."""
    release = previous_release(tags, unusable_text)
    return release if release in listed_tags(waivers_text) else None


def waived_previous_release():
    """Resolve the previous release from git and the two list files; None when it cannot be told or is not waived."""
    try:
        tags = subprocess.run(["git", "tag", "--merged", "HEAD", "--sort=version:refname", "--list", "v*"],
                              cwd=ROOT, capture_output=True, text=True, check=True).stdout.split()
        return waived_release(tags, (ROOT / UNUSABLE).read_text(encoding="utf-8"),
                              (ROOT / WAIVERS).read_text(encoding="utf-8"))
    except (OSError, subprocess.CalledProcessError):
        return None


def exclusion_summary():
    if not EXCLUDED_LANES:
        return "No integration lanes are excluded."
    return "\n".join(f"Skipped integration lane {lane} (explicitly excluded): {reason}"
                     for lane, reason in EXCLUDED_LANES.items())


def with_exclusion_summary(result):
    ok, message = result
    if ok:
        message += "\n" + exclusion_summary()
    return ok, message


def main():
    message = exclusion_summary()
    waived = waived_previous_release()
    if waived:
        message += (f"\nSkipped integration lane RollingUpgrade (stop-start release): {waived} is listed in {WAIVERS}, "
                    "so it cannot share a cluster with this build and there is no rolling upgrade to verify.")
    print(message)
    if summary := os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(summary, "a", encoding="utf-8") as file:
            file.write(message + "\n")
    if output := os.environ.get("GITHUB_OUTPUT"):
        with open(output, "a", encoding="utf-8") as file:
            file.write(f"rolling-upgrade-enabled={str('RollingUpgrade' not in EXCLUDED_LANES and not waived).lower()}\n")


if __name__ == "__main__":
    main()
