// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

public class when_naming_the_system_event_store_databases_with_a_configured_name : Specification
{
    string[] _names;

    void Because() => _names =
    [
        DatabaseNames.ForEventStore(EventStoreName.System, "run_", "chronicle-system"),
        DatabaseNames.ForEventStoreNamespace(EventStoreName.System, EventStoreNamespaceName.Default, "run_", "chronicle-system"),
        DatabaseNames.ForReadModels(EventStoreName.System, EventStoreNamespaceName.Default, "run_", "chronicle-system"),
        DatabaseNames.ForEventStore(new EventStoreName("Ada"), "run_", "chronicle-system")
    ];

    [Fact] void should_name_the_event_store_database_by_the_configured_name() => _names[0].ShouldEqual("run_chronicle-system+es");
    [Fact] void should_name_the_namespace_database_by_the_configured_name() => _names[1].ShouldEqual("run_chronicle-system+es+Default");
    [Fact] void should_name_the_read_models_by_the_configured_name() => _names[2].ShouldEqual("run_chronicle-system");
    [Fact] void should_leave_other_event_stores_named_after_themselves() => _names[3].ShouldEqual("run_Ada+es");
}
