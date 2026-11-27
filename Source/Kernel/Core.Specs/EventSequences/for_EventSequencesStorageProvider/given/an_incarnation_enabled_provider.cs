// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.given;

public class an_incarnation_enabled_provider : the_provider
{
    protected INamespaceLifecycleStorage lifecycles = null!;
    protected IHaveNamespaceIncarnations capability = null!;
    protected NamespaceLifecycle? lifecycle;
    protected NamespaceIncarnationId incarnation;
    protected GrainId grainId;
    protected IGrainState<EventSequenceState> state = null!;

    void Establish()
    {
        incarnation = new(Guid.NewGuid());
        lifecycle = NamespaceLifecycle.Open("default", incarnation, DateTimeOffset.UtcNow);
        lifecycles = Substitute.For<INamespaceLifecycleStorage>();
        capability = Substitute.For<IHaveNamespaceIncarnations>();
        capability.NamespaceLifecycles.Returns(lifecycles);
        namespaceIncarnations.Enabled.Returns(true);
        namespaceIncarnations.GetStorage(Arg.Any<EventStoreName>()).Returns(capability);
        lifecycles.Get(Arg.Any<EventStoreNamespaceName>()).Returns<Task<NamespaceLifecycle?>>(_ => Task.FromResult<NamespaceLifecycle?>(lifecycle));
        grainId = GrainId.Create("type", new EventSequenceKey("sequence", "event-store", "default").ToString());
        state = new GrainState<EventSequenceState> { State = new() };
        eventSequenceStorage.GetState().Returns(_ => Task.FromResult(new EventSequenceState()));
        eventSequenceStorage.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);
    }
}
