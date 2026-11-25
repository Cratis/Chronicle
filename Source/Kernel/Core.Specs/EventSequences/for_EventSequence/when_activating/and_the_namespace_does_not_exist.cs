// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating;

/// <summary>
/// A kernel restart wakes persisted observers, and they activate the sequence they observe. A namespace that was removed,
/// or that the store never had, must stay gone.
/// </summary>
public class and_the_namespace_does_not_exist : given.an_event_sequence
{
    Exception _exception;

    protected override async Task<EventSequence> CreateEventSequence()
    {
        _namespaces.GetCanonical(EventStoreNamespace).Returns(Task.FromResult<EventStoreNamespaceName?>(null));
        _exception = await Catch.Exception(base.CreateEventSequence);
        return null!;
    }

    [Fact] void should_activate() => _exception.ShouldBeNull();
    [Fact] async Task should_not_create_the_namespace() => await _namespaces.DidNotReceive().EnsureCanonical(EventStoreNamespace);
    [Fact] async Task should_not_ensure_the_namespace() => await _namespaces.DidNotReceive().Ensure(EventStoreNamespace);
    [Fact] async Task should_initialize_constraints() => await _constraintsGrain.Received().GetVersion();
}
