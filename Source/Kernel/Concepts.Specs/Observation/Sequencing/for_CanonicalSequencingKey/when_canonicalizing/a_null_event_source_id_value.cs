// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing;

public class a_null_event_source_id_value : Specification
{
    Events.EventSourceId _eventSourceId;
    Exception? _error;

    void Establish() => _eventSourceId = (Events.EventSourceId)RuntimeHelpers.GetUninitializedObject(typeof(Events.EventSourceId));
    void Because() => _error = Catch.Exception(() => CanonicalSequencingKey.From(SequencingKey.ForEventSourceId(_eventSourceId)));

    [Fact] void should_reject_the_value_key() => _error.ShouldBeOfExactType<InvalidSequencingKey>();
}
