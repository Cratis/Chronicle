// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_incarnations_are_disabled : given.an_incarnation_enabled_provider
{
    void Establish()
    {
        namespaceIncarnations.Enabled.Returns(false);
        eventSequenceStorage.GetState().Returns(Task.FromResult(new EventSequenceState { ActivationIncarnation = incarnation }));
    }

    Task Because() => provider.ReadStateAsync("name", grainId, state);

    [Fact] void should_leave_the_activation_incarnation_unknown() => state.State.ActivationIncarnation.ShouldBeNull();
    [Fact] void should_not_require_provider_capability() => namespaceIncarnations.DidNotReceive().GetStorage(Arg.Any<EventStoreName>());
    [Fact] void should_not_read_control_storage() => lifecycles.DidNotReceive().Get(Arg.Any<EventStoreNamespaceName>());
}
