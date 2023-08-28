// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.EventSequences.for_EventSequence.given;

public class an_event_sequence : all_dependencies
{
    protected EventSequence event_sequence;

    void Establish()
    {
        event_sequence = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            event_types.Object,
            event_serializer.Object,
            connection.Object,
            observers_registrar.Object,
            causation_manager.Object,
            identity_provider.Object,
            execution_context_manager.Object);
    }
}
