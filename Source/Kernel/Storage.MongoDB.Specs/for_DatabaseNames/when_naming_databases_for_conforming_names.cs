// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

public class when_naming_databases_for_conforming_names : Specification
{
    static readonly string[] _names = ["a", "b", "orders", "chronicle", "main", "Default", "System", "x.y", "a@b", "e", "admin2"];

    int _owners;
    int _collisions;

    void Because()
    {
        foreach (var prefix in new[] { string.Empty, "run_" })
        {
            var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Add(seen, DatabaseNames.ComposeClusterDatabase(prefix), "cluster", prefix);

            foreach (var store in _names.Where(_ => ResourceNames.ForEventStore(_) == ResourceNameViolation.None))
            {
                Add(seen, DatabaseNames.ComposeForEventStore(store, prefix), $"store:{store}", prefix);
                foreach (var @namespace in _names.Where(_ => ResourceNames.ForNamespace(_) == ResourceNameViolation.None && ResourceNames.ForCombination(store, _) == ResourceNameViolation.None))
                {
                    Add(seen, DatabaseNames.ComposeForEventStoreNamespace(store, @namespace, prefix), $"ns:{store}:{@namespace}", prefix);
                    Add(seen, DatabaseNames.ComposeForReadModels(store, @namespace, prefix), $"rm:{store}:{@namespace}", prefix);
                }
            }
        }
    }

    [Fact] void should_have_composed_names() => _owners.ShouldBeGreaterThan(100);
    [Fact] void should_never_compose_the_same_database_for_two_owners() => _collisions.ShouldEqual(0);

    void Add(Dictionary<string, string> seen, string database, string owner, string prefix)
    {
        _owners++;
        if (seen.TryGetValue(database, out var existing) && existing != owner)
        {
            _collisions++;
        }

        seen[database] = owner;
    }
}
