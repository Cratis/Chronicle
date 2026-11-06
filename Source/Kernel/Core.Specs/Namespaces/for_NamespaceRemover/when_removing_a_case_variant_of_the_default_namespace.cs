// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_removing_a_case_variant_of_the_default_namespace : given.a_namespace_remover
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(() => _remover.Remove(_eventStore, new EventStoreNamespaceName("default")));

    [Fact] void should_refuse() => _result.ShouldBeOfExactType<NamespaceCannotBeRemoved>();
    [Fact] async Task should_not_drop_anything() => await _eventStoreStorage.DidNotReceive().DropNamespace(Arg.Any<EventStoreNamespaceName>());
}
