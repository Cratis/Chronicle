// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnation;

public class when_adopting_the_listed_default_namespace : given.an_owner
{
    /// <inheritdoc/>
    protected override EventStoreNamespaceName OwnerNamespace => EventStoreNamespaceName.Default;

    void Establish() => _listing.Add(new(EventStoreNamespaceName.Default, _clock.GetUtcNow()));

    Task Because() => _owner.Ensure(EventStoreNamespaceName.Default);

    [Fact] void should_install_the_default_identity() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.Open);
    [Fact] async Task should_not_recursively_append_a_creation_notification() => await _system.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), default, default, default, default, default, default, default);
}
