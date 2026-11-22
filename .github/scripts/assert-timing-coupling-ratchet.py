# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Compatibility entry point for existing workflow and documented commands; logic lives in Node."""

from pathlib import Path
import subprocess
import sys

sys.exit(subprocess.call(["node", str(Path(__file__).with_suffix(".mjs")), *sys.argv[1:]]))
