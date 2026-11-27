// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_a_removal_callback_arrives_during_ensure : given.a_namespaces_grain
{
    readonly TaskCompletionSource<EventStoreNamespaceName> _ensure = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly NamespaceIncarnationId _identity = NamespaceIncarnationId.New();
    readonly Guid _operation = Guid.NewGuid();

    void Establish()
    {
        _state.Namespaces.Add(new NamespaceState(_namespace, DateTimeOffset.UtcNow));
        _incarnations.Enabled.Returns(true);
        _incarnations.Ensure(_eventStore, _namespace).Returns(_ensure.Task);
    }

    async Task Because()
    {
        var waiting = _namespaces.EnsureCanonical(_namespace);
        await _namespaces.ForgetIncarnation(_namespace, _identity, _operation).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _ensure.SetException(new NamespaceClosing(_namespace));
        await Catch.Exception(() => waiting);
    }

    [Fact] void should_remove_the_cached_listing_without_waiting_for_ensure() => _state.Namespaces.Count.ShouldEqual(0);
    [Fact] async Task should_validate_the_exact_callback_identity() => await _incarnations.Received(1).ValidateForget(_eventStore, _namespace, _identity, _operation);
    [Fact] void should_leave_legacy_creation_untouched() => _state.NewNamespaces.Count.ShouldEqual(0);
}
