// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames.when_getting_the_read_model_database_name;

public class and_the_namespace_is_the_default : Specification
{
    string _result;

    void Because() => _result = DatabaseNames.ForReadModels(new EventStoreName("Ada"), EventStoreNamespaceName.Default);

    [Fact] void should_use_the_bare_event_store_name() => _result.ShouldEqual("Ada");
}
