// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

public class when_naming_databases_with_an_empty_prefix : Specification
{
    string[] _names;

    void Because() => _names =
    [
        DatabaseNames.WithPrefix(WellKnownDatabaseNames.Chronicle),
        DatabaseNames.ForEventStore(new EventStoreName("Ada"), string.Empty),
        DatabaseNames.ForEventStoreNamespace(new EventStoreName("Ada"), EventStoreNamespaceName.Default, string.Empty),
        DatabaseNames.ForReadModels(new EventStoreName("Ada"), EventStoreNamespaceName.Default, string.Empty),
        DatabaseNames.ForReadModels(new EventStoreName("Ada"), new EventStoreNamespaceName("Contoso"), string.Empty)
    ];

    [Fact] void should_leave_the_global_database_unchanged() => _names[0].ShouldEqual("chronicle+main");
    [Fact] void should_leave_the_event_store_database_unchanged() => _names[1].ShouldEqual("Ada+es");
    [Fact] void should_leave_the_namespace_database_unchanged() => _names[2].ShouldEqual("Ada+es+Default");
    [Fact] void should_leave_default_read_models_unchanged() => _names[3].ShouldEqual("Ada");
    [Fact] void should_leave_named_namespace_read_models_unchanged() => _names[4].ShouldEqual("Ada+Contoso");
}
