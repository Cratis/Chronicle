// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

public class when_getting_the_event_sequence_database_names : Specification
{
    string _eventStore;
    string _defaultNamespace;
    string _namedNamespace;

    void Because()
    {
        _eventStore = DatabaseNames.ForEventStore(new EventStoreName("Ada"));
        _defaultNamespace = DatabaseNames.ForEventStoreNamespace(new EventStoreName("Ada"), EventStoreNamespaceName.Default);
        _namedNamespace = DatabaseNames.ForEventStoreNamespace(new EventStoreName("Ada"), new EventStoreNamespaceName("Contoso"));
    }

    [Fact] void should_suffix_the_event_store_database() => _eventStore.ShouldEqual("Ada+es");

    /// <summary>
    /// Unlike read models, event sequences suffix the default namespace too. That asymmetry is the one a reader
    /// composing "&lt;eventStore&gt;+&lt;namespace&gt;" for both trips over.
    /// </summary>
    [Fact] void should_suffix_the_default_namespace() => _defaultNamespace.ShouldEqual("Ada+es+Default");

    [Fact] void should_suffix_a_named_namespace() => _namedNamespace.ShouldEqual("Ada+es+Contoso");
}
