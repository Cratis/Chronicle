// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing_an_unresolved_key;

public class with_a_null_event_type_value : Specification
{
    Events.EventTypeId _eventType;
    Exception? _error;

    void Establish() => _eventType = (Events.EventTypeId)RuntimeHelpers.GetUninitializedObject(typeof(Events.EventTypeId));
    void Because() => _error = Catch.Exception(() => SequencingKey.Unresolved(_eventType));

    [Fact] void should_reject_the_missing_event_type() => _error.ShouldBeOfExactType<InvalidSequencingKey>();
}
