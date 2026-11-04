// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

public class when_naming_the_system_event_store_databases_by_the_legacy_name : Specification
{
    string[] _names;

    void Because() => _names =
    [
        DatabaseNames.ForEventStore(EventStoreName.System, systemDatabaseName: DatabaseNames.LegacySystemDatabaseName),
        DatabaseNames.ForEventStoreNamespace(EventStoreName.System, EventStoreNamespaceName.Default, systemDatabaseName: DatabaseNames.LegacySystemDatabaseName),
        DatabaseNames.ForReadModels(EventStoreName.System, EventStoreNamespaceName.Default, systemDatabaseName: DatabaseNames.LegacySystemDatabaseName)
    ];

    [Fact] void should_name_the_event_store_database_as_it_used_to_be() => _names[0].ShouldEqual("System+es");
    [Fact] void should_name_the_namespace_database_as_it_used_to_be() => _names[1].ShouldEqual("System+es+Default");
    [Fact] void should_name_the_read_models_as_they_used_to_be() => _names[2].ShouldEqual("System");
}
