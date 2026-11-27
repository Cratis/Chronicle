// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_the_incarnation_changes_during_hydration : given.an_incarnation_enabled_provider
{
    void Establish()
    {
        eventSequenceStorage.GetState().Returns(_ =>
        {
            lifecycle = NamespaceLifecycle.Open("default", new NamespaceIncarnationId(Guid.NewGuid()), DateTimeOffset.UtcNow);
            return Task.FromResult(new EventSequenceState { ActivationIncarnation = lifecycle.Incarnation });
        });
        eventSequenceStorage.GetTailSequenceNumber().Returns(Task.FromResult<EventSequenceNumber>(42));
    }

    Task Because() => provider.ReadStateAsync("name", grainId, state);

    [Fact] void should_keep_the_incarnation_captured_before_loading_state() => state.State.ActivationIncarnation.ShouldEqual(incarnation);
    [Fact] void should_not_refresh_the_incarnation() => lifecycles.Received(1).Get(Arg.Any<EventStoreNamespaceName>());
    [Fact] void should_load_the_retained_tail() => state.State.SequenceNumber.ShouldEqual((EventSequenceNumber)43);

    [Fact]
    void should_capture_before_opening_namespace_storage() => Received.InOrder(() =>
    {
        lifecycles.Get(Arg.Any<EventStoreNamespaceName>());
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>());
        eventSequenceStorage.GetState();
    });
}
