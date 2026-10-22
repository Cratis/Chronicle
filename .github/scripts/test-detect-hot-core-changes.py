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


class HotCoreContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.root = detector.git(Path(__file__).parent, "rev-parse", "--show-toplevel").strip()
        cls.contract = detector.read_contract(cls.root)

    def test_5275_runtime_paths_are_hot_core(self):
        paths = {
            "Storage.Sql/EventStores/Migrations/v19_107_0.cs": "SQL storage and migrations",
            "Storage.Sql/Database.cs": "SQL storage and migrations",
            "Storage/IEventStoreStorage.cs": "Storage abstractions",
            "Storage/Generations/GenerationFloorExtensions.cs": "Storage abstractions",
            "Storage.InMemory/EventStoreStorage.cs": "In-memory storage",
            "Storage.MongoDB/MongoDBKernelStateResetHandler.cs": "MongoDB storage",
            "Core/Generations/Admission/GenerationFloorAdmission.cs": "Kernel grains and services",
            "Core/Setup/ChronicleServerStartupTask.cs": "Kernel grains and services",
            "Concepts/Projections/Generations/Enablement/GenerationFloor.cs": "Kernel concepts",
        }
        for path, area in paths.items():
            with self.subTest(path=path):
                full_path = "Source/Kernel/" + path
                self.assertEqual({area: [full_path]}, detector.matches(self.contract, [full_path]))

    def test_specs_and_out_of_scope_paths_are_not_hot_core(self):
        paths = [
            "Concepts.Specs/Projections/Generations/Enablement/when_constructing_a_floor.cs",
            "Core.Specs/Generations/Admission/for_GenerationFloorAdmission/when_all_stores_are_supported.cs",
            "Storage.Specs/Generations/for_GenerationFloorExtensions/when_admitting_an_unsupported_floor.cs",
            "Storage.Sql.Specs/Generations/for_GenerationFloorStorage/when_resetting/on_sqlite.cs",
            "Storage.InMemory.Specs/Generations/for_GenerationFloorStorage/when_changing_incarnation.cs",
            "Storage.MongoDB.Specs/for_MongoDBKernelStateResetHandler/when_a_generation_floor_is_present.cs",
            "Server/Program.cs", "Grpc/Events.cs", "Contracts/Events.cs",
            "Storage.Vault/Store.cs", "Storage.AzureKeyVault/Store.cs",
        ]
        for path in ["Source/Kernel/" + path for path in paths] + ["Documentation/index.md"]:
            with self.subTest(path=path):
                self.assertEqual({}, detector.matches(self.contract, [path]))

    def test_storage_boundary_and_changeset_contract(self):
        for path, area in [
            ("Source/Kernel/Storage/x.cs", "Storage abstractions"),
            ("Source/Kernel/Storage.Sql/x.cs", "SQL storage and migrations"),
            ("Source/Infrastructure/Changes/Changeset.cs", "Changeset consolidation"),
        ]:
            with self.subTest(path=path):
                self.assertEqual({area: [path]}, detector.matches(self.contract, [path]))

    def test_narrow_areas_win_without_double_counting(self):
        paths = ["Source/Kernel/Core/Observation/Observer.cs", "Source/Kernel/Storage.Sql/Sinks/Sink.cs"]
        self.assertEqual({"Observer handling": [paths[0]], "SQL sink": [paths[1]]},
                         detector.matches(self.contract, paths + paths))


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
