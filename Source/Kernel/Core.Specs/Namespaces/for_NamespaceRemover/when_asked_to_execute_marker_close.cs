// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_asked_to_execute_marker_close : given.a_namespace_remover
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _remover.Execute(_eventStore, _namespace, NamespaceRemovalStage.CloseMarker));

    [Fact] void should_require_the_lifecycle_owner_to_close_the_marker() => _error.ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] async Task should_not_forget_the_namespace() => await _namespaces.DidNotReceive().Forget(_namespace);
    [Fact] async Task should_not_drop_data() => await _eventStoreStorage.DidNotReceive().DropNamespace(_namespace);
}
