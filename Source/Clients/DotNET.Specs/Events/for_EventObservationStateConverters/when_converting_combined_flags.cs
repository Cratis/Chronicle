// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventObservationStateConverters;

public class when_converting_combined_flags : Specification
{
    Contracts.Events.EventObservationState _contractState;
    EventObservationState _clientState;

    void Because()
    {
        _contractState = (EventObservationState.Initial | EventObservationState.Replay).ToContract();
        _clientState = ((Contracts.Events.EventObservationState)(EventObservationState.Initial | EventObservationState.Replay)).ToClient();
    }

    [Fact] void should_preserve_both_flags_in_the_contract() => _contractState.ShouldEqual((Contracts.Events.EventObservationState)(EventObservationState.Initial | EventObservationState.Replay));
    [Fact] void should_preserve_both_flags_in_the_client() => _clientState.ShouldEqual(EventObservationState.Initial | EventObservationState.Replay);
}
