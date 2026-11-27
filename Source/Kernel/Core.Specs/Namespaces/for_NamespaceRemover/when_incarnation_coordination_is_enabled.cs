// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_incarnation_coordination_is_enabled : given.a_namespace_remover
{
    void Establish() => _incarnations.Enabled.Returns(true);

    Task Because() => _remover.Remove(_eventStore, _namespace);

    [Fact] async Task should_delegate_to_the_lifecycle_owner() => await _incarnations.Received(1).Remove(_eventStore, _namespace);
    [Fact] async Task should_not_drop_without_the_marker_checkpoint() => await _eventStoreStorage.DidNotReceive().DropNamespace(_namespace);
    [Fact] async Task should_not_use_the_legacy_forget_callback() => await _namespaces.DidNotReceive().Forget(_namespace);
}
