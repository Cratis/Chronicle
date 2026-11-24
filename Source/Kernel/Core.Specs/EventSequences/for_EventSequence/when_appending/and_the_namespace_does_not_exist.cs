// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

/// <summary>
/// A client appending to a namespace of its own store, such as a new tenant, is what brings the namespace into being.
/// </summary>
public class and_the_namespace_does_not_exist : given.an_event_sequence
{
    AppendResult _result;

    protected override async Task<EventSequence> CreateEventSequence()
    {
        _namespaces.GetCanonical(EventStoreNamespace).Returns(Task.FromResult<EventStoreNamespaceName?>(null));
        return await base.CreateEventSequence();
    }

    async Task Because()
    {
        _result = await AppendAnEvent();
        await AppendAnEvent();
    }

    [Fact] void should_append() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_create_the_namespace_once() => await _namespaces.Received(1).EnsureCanonical(EventStoreNamespace);
}
