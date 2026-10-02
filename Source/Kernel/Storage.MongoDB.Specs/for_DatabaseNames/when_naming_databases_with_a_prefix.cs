// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

public class when_naming_databases_with_a_prefix : Specification
{
    string[] _names;

    void Because() => _names =
    [
        DatabaseNames.WithPrefix(WellKnownDatabaseNames.Chronicle, "run_"),
        DatabaseNames.ForEventStore(new EventStoreName("Ada"), "run_"),
        DatabaseNames.ForEventStoreNamespace(new EventStoreName("Ada"), EventStoreNamespaceName.Default, "run_"),
        DatabaseNames.ForReadModels(new EventStoreName("Ada"), EventStoreNamespaceName.Default, "run_"),
        DatabaseNames.ForReadModels(new EventStoreName("Ada"), new EventStoreNamespaceName("Contoso"), "run_")
    ];

    [Fact] void should_prefix_the_global_database() => _names[0].ShouldEqual("run_chronicle+main");
    [Fact] void should_prefix_the_event_store_database() => _names[1].ShouldEqual("run_Ada+es");
    [Fact] void should_prefix_the_namespace_database() => _names[2].ShouldEqual("run_Ada+es+Default");
    [Fact] void should_prefix_default_read_models_without_adding_a_namespace_suffix() => _names[3].ShouldEqual("run_Ada");
    [Fact] void should_prefix_named_namespace_read_models() => _names[4].ShouldEqual("run_Ada+Contoso");
}
