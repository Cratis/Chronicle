# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Check the hot-core summary for ordinary and requested integration runs."""

import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).with_name("detect-hot-core-changes.py")
spec = importlib.util.spec_from_file_location("detect_hot_core_changes", SCRIPT)
detector = importlib.util.module_from_spec(spec)
spec.loader.exec_module(detector)


class HotCoreSummary(unittest.TestCase):
    def test_ordinary_push_requests_integration_without_claiming_it_ran(self):
        with patch.object(detector, "emit") as emit:
            touched = detector.report({"Observer handling": ["Source/Kernel/Core/Observation/Observer.cs"]},
                                      ["Source/Kernel/Core/Observation/Observer.cs"])
        summary = "\n".join(call.args[0] for call in emit.call_args_list)
        self.assertTrue(touched)
        self.assertIn("request integration for this change", summary)
        self.assertIn("run-integration", summary)
        self.assertIn("Run the relevant integration namespace locally", summary)
        self.assertNotIn("matrix runs", summary)

    def test_requested_run_reports_the_full_matrix(self):
        with patch.object(detector, "emit") as emit:
            touched = detector.report({"Observer handling": ["Source/Kernel/Core/Observation/Observer.cs"]},
                                      ["Source/Kernel/Core/Observation/Observer.cs"], run_integration=True)
        summary = "\n".join(call.args[0] for call in emit.call_args_list)
        self.assertTrue(touched)
        self.assertIn("full out-of-process integration matrix runs", summary)
        self.assertNotIn("apply the `run-integration` label", summary)

    def test_unaffected_push_does_not_request_the_matrix(self):
        with patch.object(detector, "emit") as emit:
            self.assertFalse(detector.report({}, ["Documentation/contributing/integration-tests.md"]))
        summary = "\n".join(call.args[0] for call in emit.call_args_list)
        self.assertIn("no hot-core path touched", summary)
        self.assertNotIn("run-integration", summary)


if __name__ == "__main__":
    unittest.main()
