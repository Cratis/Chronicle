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
        self.write("server_two", "SqlServerCollection", "server_name_two")
        self.write("server_three", "SqlServerCollection", "server_name_three")

    def write(self, file, collection, name):
        path = self.root / f"{file}.cs"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(f"namespace Specs;\n[Collection({collection}.Name)]\n"
                        f"public class {name} : given.a_database {{ }}", encoding="utf-8")

    def test_every_spec_has_exactly_one_shard_including_new_uncollected_specs(self):
        classes, weights = filters.provider_classes(self.root)
        expressions = {shard: filters.test_filter(classes, weights, shard)
                       for shard in filters.shard_names()}
        for name, expected in (("Specs.neutral_name.should_store", "postgresql"),
                               ("Specs.server_name.should_store", "sqlserver-1"),
                               ("Specs.sqlite.should_store", "remaining"),
                               ("Specs.new_test.should_store", "remaining"),
                               ("Specs.neutral_name_extended.should_store", "remaining")):
            with self.subTest(name=name):
                self.assertEqual([expected], [shard for shard, expression in expressions.items()
                                              if selected(expression, name)])

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
        self.assertEqual(4, sum(map(len, filters.provider_classes(self.root)[0].values())))

    def test_repository_collections_are_nonempty_and_disjoint(self):
        classes, _ = filters.provider_classes(filters.SPEC_ROOT)
        self.assertTrue(set(classes["postgresql"]).isdisjoint(classes["sqlserver"]))

    def test_a_sharded_provider_is_split_into_disjoint_shards_that_cover_it_exactly(self):
        classes, weights = filters.provider_classes(filters.SPEC_ROOT)
        count = filters.SHARDS["sqlserver"]
        shards = filters.partition(classes["sqlserver"], weights, count)
        self.assertEqual(count, len(shards))
        self.assertTrue(all(shards))
        self.assertEqual(sorted(classes["sqlserver"]), sorted(name for shard in shards for name in shard))
        self.assertEqual(len(classes["sqlserver"]), len({name for shard in shards for name in shard}))

    def test_shards_are_balanced_and_stable(self):
        classes, weights = filters.provider_classes(filters.SPEC_ROOT)
        shards = filters.partition(classes["sqlserver"], weights, filters.SHARDS["sqlserver"])
        totals = [sum(weights[name] for name in shard) for shard in shards]
        heaviest = max(weights[name] for name in classes["sqlserver"])
        self.assertLessEqual(max(totals) - min(totals), heaviest)
        self.assertEqual(shards, filters.partition(list(reversed(classes["sqlserver"])), weights, len(shards)))

    def test_a_shard_with_no_classes_fails_instead_of_selecting_every_spec(self):
        classes, weights = filters.provider_classes(self.root)
        classes["sqlserver"] = classes["sqlserver"][:2]
        with self.assertRaisesRegex(ValueError, "selects no spec classes"):
            filters.test_filter(classes, weights, "sqlserver-3")

    def test_the_matrix_lists_exactly_the_shards_the_script_accepts(self):
        self.assertEqual(["postgresql", "sqlserver-1", "sqlserver-2", "sqlserver-3", "remaining"],
                         filters.shard_names())


if __name__ == "__main__":
    unittest.main()
