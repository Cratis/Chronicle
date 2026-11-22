// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_executing_the_forget_checkpoint : given.a_namespace_remover
{
    Task Because() => _remover.Execute(_eventStore, _namespace, NamespaceRemovalStage.Forget);

    [Fact] async Task should_forget_the_listing() => await _namespaces.Received(1).Forget(_namespace);
    [Fact] async Task should_not_start_cleanup() => await _observer.DidNotReceive().Unsubscribe();
    [Fact] async Task should_not_drop_data() => await _eventStoreStorage.DidNotReceive().DropNamespace(_namespace);
    [Fact] async Task should_not_delete_the_record() => await _namespaceRecords.DidNotReceive().Delete(_namespace);
}
