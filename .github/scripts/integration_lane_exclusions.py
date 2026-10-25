# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Reviewed lane exclusions, never inferred from a zero-execution test result."""

import os

# Remove the entry when the rolling-upgrade spec is re-enabled; no runner waiver is needed.
EXCLUDED_LANES = {
    "RollingUpgrade": "Reactors miss acknowledged events after a one-node-at-a-time upgrade; re-enable after https://github.com/Cratis/Chronicle/issues/5300 and https://github.com/Cratis/Chronicle/issues/5350 are fixed.",
}


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
    print(message)
    if summary := os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(summary, "a", encoding="utf-8") as file:
            file.write(message + "\n")
    if output := os.environ.get("GITHUB_OUTPUT"):
        with open(output, "a", encoding="utf-8") as file:
            file.write(f"rolling-upgrade-enabled={str('RollingUpgrade' not in EXCLUDED_LANES).lower()}\n")


if __name__ == "__main__":
    main()
