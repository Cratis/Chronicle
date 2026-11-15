// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

/// <summary>
/// Names that could alias the System, cluster or database-internal databases are rejected on every resolution.
/// </summary>
public class when_an_event_store_has_a_reserved_or_malformed_name : Specification
{
    static readonly string[] _rejected =
    [
        "_system+es+Default",
        "_SYSTEM+ES+DEFAULT",
        "_system+anything",
        "chronicle+main",
        "admin",
        "Local",
        "CONFIG"
    ];

    static readonly string[] _accepted = ["es1", "adminx", "configuration", "localhost"];

    int _rejectedCount;
    int _acceptedCount;
    int _customRejectedCount;
    string _system;
    string _legacy;

    void Because()
    {
        foreach (var name in _rejected)
        {
            var store = new EventStoreName(name);
            _rejectedCount += Rejects(() => DatabaseNames.ForEventStore(store));
            _rejectedCount += Rejects(() => DatabaseNames.ForEventStoreNamespace(store, EventStoreNamespaceName.Default));
            _rejectedCount += Rejects(() => DatabaseNames.ForReadModels(store, EventStoreNamespaceName.Default));
            _customRejectedCount += Rejects(() => DatabaseNames.ForEventStore(store, "run_", "sys"));
        }

        _customRejectedCount += Rejects(() => DatabaseNames.ForEventStore(new EventStoreName("sys+es+Default"), "run_", "sys"));
        foreach (var name in _accepted)
        {
            var store = new EventStoreName(name);
            DatabaseNames.ForEventStore(store);
            DatabaseNames.ForEventStoreNamespace(store, EventStoreNamespaceName.Default);
            DatabaseNames.ForReadModels(store, EventStoreNamespaceName.Default);
            _acceptedCount++;
        }

        _system = DatabaseNames.ForEventStore(EventStoreName.System);
        _legacy = DatabaseNames.ForEventStore(EventStoreName.System, systemDatabaseName: DatabaseNames.LegacySystemDatabaseName);
    }

    [Fact] void should_reject_every_reserved_name_everywhere() => _rejectedCount.ShouldEqual(_rejected.Length * 3);
    [Fact] void should_reject_with_a_prefix_and_a_configured_system_name() => _customRejectedCount.ShouldEqual(_rejected.Length + 1);
    [Fact] void should_accept_ordinary_names() => _acceptedCount.ShouldEqual(_accepted.Length);
    [Fact] void should_still_resolve_the_system_event_store() => _system.ShouldEqual("_system+es");
    [Fact] void should_still_resolve_the_legacy_system_name() => _legacy.ShouldEqual("System+es");

    static int Rejects(Func<string> resolve) => Catch.Exception(() => resolve()) is InvalidDatabaseName ? 1 : 0;
}
