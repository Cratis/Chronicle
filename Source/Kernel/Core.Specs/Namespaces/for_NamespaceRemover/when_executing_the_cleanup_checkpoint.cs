// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_executing_the_cleanup_checkpoint : given.a_namespace_remover
{
    Task Because() => _remover.Execute(_eventStore, _namespace, NamespaceRemovalStage.Cleanup);

    [Fact] async Task should_unsubscribe_the_observers() => await _observer.Received(1).Unsubscribe();
    [Fact] async Task should_remove_the_observers() => await _observer.Received(1).Remove();
    [Fact] async Task should_not_repeat_forget() => await _namespaces.DidNotReceive().Forget(_namespace);
    [Fact] async Task should_not_drop_data() => await _eventStoreStorage.DidNotReceive().DropNamespace(_namespace);
}
