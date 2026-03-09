#!/usr/bin/env python3
"""Restructure eval workspace: move grading.json into run-1/ dirs and add summary fields."""
import json
import shutil
from pathlib import Path

base = Path("/Volumes/sourcecode/repos/cratis/Documentation/.github/skills/skills-eval-workspace/iteration-1")

for grading_file in sorted(base.rglob("grading.json")):
    parent = grading_file.parent
    if parent.name not in ("with_skill", "without_skill"):
        continue

    with open(grading_file) as f:
        grading = json.load(f)

    expectations = grading.get("expectations", [])
    passed = sum(1 for e in expectations if e.get("passed", False))
    failed = len(expectations) - passed
    total = len(expectations)
    pass_rate = round(passed / total, 4) if total > 0 else 0.0

    grading["summary"] = {
        "pass_rate": pass_rate,
        "passed": passed,
        "failed": failed,
        "total": total
    }

    run_dir = parent / "run-1"
    run_dir.mkdir(exist_ok=True)

    with open(run_dir / "grading.json", "w") as f:
        json.dump(grading, f, indent=2)

    timing_file = parent / "timing.json"
    if timing_file.exists():
        shutil.copy(timing_file, run_dir / "timing.json")

    print(f"OK {parent.parent.parent.name}/{parent.parent.name}/{parent.name} pass_rate={pass_rate} ({passed}/{total})")

print("Done!")
