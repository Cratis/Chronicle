// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseIdentity;

public class when_naming_logical_databases_for_conforming_names : Specification
{
    static readonly string[] _names = ["a", "b", "orders", "chronicle", "main", "Default", "System", "x.y", "a@b", "a b"];

    int _owners;
    int _collisions;

    void Because()
    {
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Add(seen, DatabaseIdentity.Cluster, "chronicle");

        foreach (var store in _names.Where(_ => ResourceNames.ForEventStore(_) == ResourceNameViolation.None))
        {
            Add(seen, DatabaseIdentity.Store(store), "chronicle");
            foreach (var @namespace in _names.Where(_ => ResourceNames.ForNamespace(_) == ResourceNameViolation.None))
            {
                Add(seen, new DatabaseIdentity(DatabaseKind.Namespace, store, @namespace), "chronicle");
                Add(seen, new DatabaseIdentity(DatabaseKind.ReadModels, store, @namespace), "chronicle");
            }
        }
    }

    [Fact] void should_have_composed_names() => _owners.ShouldBeGreaterThan(100);
    [Fact] void should_never_compose_the_same_logical_name_for_two_owners() => _collisions.ShouldEqual(0);

    void Add(Dictionary<string, string> seen, DatabaseIdentity identity, string cluster)
    {
        _owners++;
        var owner = $"{identity.Kind}:{identity.EventStore}:{identity.Namespace}";
        var name = identity.LogicalName(cluster);
        if (seen.TryGetValue(name, out var existing) && existing != owner)
        {
            _collisions++;
        }

        seen[name] = owner;
    }
}
