// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public abstract class an_event_sequence_with_a_registered_visibility : appending_many_events
{
    protected abstract EventTypeVisibility Visibility { get; }

    void Establish() =>
        _eventTypesStorage.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>())
            .Returns(new EventTypeSchema(_eventType, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema(), Visibility));
}
