// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_a_checkpoint_targets_the_default_namespace : given.a_namespace_remover
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _remover.Execute(_eventStore, EventStoreNamespaceName.Default, NamespaceRemovalStage.Drop));

    [Fact] void should_refuse_the_checkpoint() => _error.ShouldBeOfExactType<NamespaceCannotBeRemoved>();
    [Fact] async Task should_not_drop_data() => await _eventStoreStorage.DidNotReceive().DropNamespace(Arg.Any<EventStoreNamespaceName>());
}
