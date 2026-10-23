# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Exercise SQL shard coverage, collection parsing and fail-closed behavior."""

import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("sql_spec_filter", Path(__file__).with_name("sql-spec-filter.py"))
filters = importlib.util.module_from_spec(spec)
spec.loader.exec_module(filters)


def selected(expression, name):
    return any(all((term.split("!~", 1)[1] not in name) if "!~" in term
                   else (term.split("~", 1)[1] in name)
                   for term in group.split("&")) for group in expression.split("|"))


class SqlSpecFilters(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.root = Path(directory.name)
        self.write("postgres", "PostgreSqlCollection", "neutral_name")
        self.write("server", "SqlServerCollection", "server_name")

    def write(self, file, collection, name):
        path = self.root / f"{file}.cs"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(f"namespace Specs;\n[Collection({collection}.Name)]\n"
                        f"public class {name} : given.a_database {{ }}", encoding="utf-8")

    def test_every_spec_has_exactly_one_shard_including_new_uncollected_specs(self):
        classes = filters.provider_classes(self.root)
        expressions = {shard: filters.test_filter(classes, shard)
                       for shard in (*filters.COLLECTIONS, "remaining")}
        for name, expected in (("Specs.neutral_name.should_store", "postgresql"),
                               ("Specs.server_name.should_store", "sqlserver"),
                               ("Specs.sqlite.should_store", "remaining"),
                               ("Specs.new_test.should_store", "remaining"),
                               ("Specs.neutral_name_extended.should_store", "remaining")):
            with self.subTest(name=name):
                self.assertEqual([expected], [shard for shard, expression in expressions.items()
                                              if selected(expression, name)])

    def test_every_sql_server_class_lands_in_exactly_one_part(self):
        for index in range(9):
            self.write(f"server{index}", "SqlServerCollection", f"server_{index}")
        classes = filters.provider_classes(self.root)
        parts = [shard for shard in filters.SHARDS if shard.startswith("sqlserver-")]
        self.assertEqual(filters.SQLSERVER_PARTS, len(parts))
        expressions = {part: filters.test_filter(classes, part) for part in parts}
        for name in classes["sqlserver"]:
            with self.subTest(name=name):
                self.assertEqual(1, sum(selected(expression, name + "should_store")
                                        for expression in expressions.values()))
        for name in classes["postgresql"]:
            self.assertFalse(any(selected(expression, name + "should_store")
                                 for expression in expressions.values()))

    def test_workflow_matrix_lists_every_shard(self):
        workflow = (filters.SPEC_ROOT.parents[2] / ".github/workflows/dotnet-build.yml").read_text(encoding="utf-8")
        self.assertIn("shard: [" + ", ".join(filters.SHARDS) + "]", workflow)

    def test_repository_sql_server_parts_are_nonempty(self):
        classes = filters.provider_classes(filters.SPEC_ROOT)
        for shard in filters.SHARDS:
            if shard.startswith("sqlserver-"):
                self.assertTrue(filters.test_filter(classes, shard))

    def test_unknown_sql_server_part_fails(self):
        with self.assertRaisesRegex(ValueError, "Unknown SQL Server part"):
            filters.test_filter(filters.provider_classes(self.root), "sqlserver-9")

    def test_missing_provider_fails(self):
        (self.root / "postgres.cs").unlink()
        with self.assertRaisesRegex(ValueError, "nonempty, unique postgresql"):
            filters.provider_classes(self.root)

    def test_unknown_collection_fails(self):
        self.write("unknown", "AnotherCollection", "unknown")
        with self.assertRaisesRegex(ValueError, "Unknown SQL spec collection"):
            filters.provider_classes(self.root)

    def test_unsupported_declaration_fails_instead_of_misrouting(self):
        (self.root / "postgres.cs").write_text(
            'namespace Specs;\n[Collection("postgres")]\npublic class neutral_name {}', encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "Unsupported collection declaration"):
            filters.provider_classes(self.root)

    def test_duplicate_class_fails(self):
        self.write("duplicate", "PostgreSqlCollection", "neutral_name")
        with self.assertRaisesRegex(ValueError, "nonempty, unique postgresql"):
            filters.provider_classes(self.root)

    def test_generated_obj_sources_are_ignored(self):
        self.write("obj/generated", "AnotherCollection", "generated")
        self.assertEqual(2, sum(map(len, filters.provider_classes(self.root).values())))

    def test_repository_collections_are_nonempty_and_disjoint(self):
        classes = filters.provider_classes(filters.SPEC_ROOT)
        self.assertTrue(set(classes["postgresql"]).isdisjoint(classes["sqlserver"]))


if __name__ == "__main__":
    unittest.main()
