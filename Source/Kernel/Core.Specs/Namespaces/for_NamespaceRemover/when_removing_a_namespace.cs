// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_removing_a_namespace : given.a_namespace_remover
{
    async Task Because() => await _remover.Remove(_eventStore, _namespace);

    [Fact] async Task should_unsubscribe_the_observer() => await _observer.Received(1).Unsubscribe();
    [Fact] async Task should_remove_the_observer() => await _observer.Received(1).Remove();
    [Fact] async Task should_drop_the_data_of_the_namespace() => await _eventStoreStorage.Received(1).DropNamespace(_namespace);
    [Fact] async Task should_delete_the_namespace_record() => await _namespaceRecords.Received(1).Delete(_namespace);
    [Fact] async Task should_forget_the_namespace() => await _namespaces.Received(1).Forget(_namespace);
}
