// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_executing_the_delete_listing_checkpoint : given.a_namespace_remover
{
    void Establish() => _eventStoreStorage.ClearReceivedCalls();

    Task Because() => _remover.Execute(_eventStore, _namespace, NamespaceRemovalStage.DeleteListing);

    [Fact] async Task should_delete_the_namespace_record() => await _namespaceRecords.Received(1).Delete(_namespace);
    [Fact] void should_not_reopen_the_namespace_database_after_drop() => _eventStoreStorage.DidNotReceive().GetNamespace(_namespace);
    [Fact] async Task should_not_repeat_the_drop() => await _eventStoreStorage.DidNotReceive().DropNamespace(_namespace);
}
