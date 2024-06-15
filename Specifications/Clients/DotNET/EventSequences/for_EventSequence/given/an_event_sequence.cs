// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence : all_dependencies
{
    protected EventSequence event_sequence;

    void Establish()
    {
        event_sequence = new(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            connection.Object,
            event_types.Object,
            event_serializer.Object,
            causation_manager.Object,
            identity_provider.Object);
    }
}
