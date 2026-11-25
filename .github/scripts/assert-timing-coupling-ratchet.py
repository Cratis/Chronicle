# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Compatibility entry point for existing workflow and documented commands; logic lives in Node."""

from pathlib import Path
import subprocess
import sys

try:
    script = Path(__file__).with_suffix(".mjs")
    with script.open("rb"):
        pass
    status = subprocess.call(["node", str(script), *sys.argv[1:]])
except OSError as error:
    print(f"Cannot run timing-coupling ratchet: {error}", file=sys.stderr)
    status = 2

sys.exit(status)
