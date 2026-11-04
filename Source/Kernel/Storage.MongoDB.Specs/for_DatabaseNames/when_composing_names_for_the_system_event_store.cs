// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

public class when_composing_names_for_the_system_event_store : Specification
{
    string[] _system;
    string[] _namedLikeTheSystemDatabase;

    void Because()
    {
        _system =
        [
            DatabaseNames.ComposeForEventStore(EventStoreName.System, "run_", "chronicle-system"),
            DatabaseNames.ComposeForEventStoreNamespace(EventStoreName.System, EventStoreNamespaceName.Default, "run_", "chronicle-system"),
            DatabaseNames.ComposeForReadModels(EventStoreName.System, EventStoreNamespaceName.Default, "run_", "chronicle-system")
        ];

        _namedLikeTheSystemDatabase =
        [
            DatabaseNames.ComposeForEventStore(new EventStoreName("chronicle-system"), "run_", "chronicle-system"),
            DatabaseNames.ComposeForEventStoreNamespace(new EventStoreName("chronicle-system"), EventStoreNamespaceName.Default, "run_", "chronicle-system"),
            DatabaseNames.ComposeForReadModels(new EventStoreName("chronicle-system"), EventStoreNamespaceName.Default, "run_", "chronicle-system")
        ];
    }

    [Fact] void should_compose_the_event_store_database_from_the_configured_name() => _system[0].ShouldEqual("run_chronicle-system+es");
    [Fact] void should_compose_the_namespace_database_from_the_configured_name() => _system[1].ShouldEqual("run_chronicle-system+es+Default");
    [Fact] void should_compose_the_read_models_database_from_the_configured_name() => _system[2].ShouldEqual("run_chronicle-system");
    [Fact] void should_not_throw_for_a_store_named_like_the_system_database() => _namedLikeTheSystemDatabase.SequenceEqual(_system).ShouldBeTrue();
}
