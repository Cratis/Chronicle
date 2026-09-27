// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_redacting_many;

public class and_causation_properties_are_omitted : given.an_event_sequence_omitting_causation_properties
{
    async Task Because()
    {
        await _eventSequence.Redact(_eventSourceId, RedactionReason.Unknown, [_eventType], CorrelationId.New(), _requestedCausation, Identity.System);
        _storedCausation = StoredFor("Redact", 4);
    }

    [Fact] void should_omit_the_operations_causation_properties() => _storedCausation.All(causation => causation.Properties.Count == 0).ShouldBeTrue();
    [Fact] void should_keep_type_and_time() => _storedCausation.Select(causation => (causation.Type, causation.Occurred)).ShouldEqual(_requestedCausation.Select(causation => (causation.Type, causation.Occurred)));
}
