// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames.when_getting_the_read_model_database_name;

public class and_the_namespace_is_named : Specification
{
    string _result;

    void Because() => _result = DatabaseNames.ForReadModels(new EventStoreName("Ada"), new EventStoreNamespaceName("Contoso"));

    [Fact] void should_suffix_the_event_store_name_with_the_namespace() => _result.ShouldEqual("Ada+Contoso");
}
