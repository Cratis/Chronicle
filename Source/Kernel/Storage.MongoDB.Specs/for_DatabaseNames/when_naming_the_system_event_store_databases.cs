// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

public class when_naming_the_system_event_store_databases : Specification
{
    string[] _names;

    void Because() => _names =
    [
        DatabaseNames.ForEventStore(EventStoreName.System),
        DatabaseNames.ForEventStoreNamespace(EventStoreName.System, EventStoreNamespaceName.Default),
        DatabaseNames.ForReadModels(EventStoreName.System, EventStoreNamespaceName.Default),
        DatabaseNames.ForReadModels(EventStoreName.System, new EventStoreNamespaceName("Contoso")),
        DatabaseNames.ForEventStore(new EventStoreName("system")),
        DatabaseNames.ForEventStore(EventStoreName.System, "run_")
    ];

    [Fact] void should_name_the_event_store_database_by_the_system_database_name() => _names[0].ShouldEqual("_system+es");
    [Fact] void should_name_the_namespace_database_by_the_system_database_name() => _names[1].ShouldEqual("_system+es+Default");
    [Fact] void should_name_the_default_read_models_by_the_system_database_name() => _names[2].ShouldEqual("_system");
    [Fact] void should_name_named_namespace_read_models_by_the_system_database_name() => _names[3].ShouldEqual("_system+Contoso");
    [Fact] void should_recognize_the_system_event_store_regardless_of_case() => _names[4].ShouldEqual("_system+es");
    [Fact] void should_put_the_prefix_first() => _names[5].ShouldEqual("run__system+es");
}
