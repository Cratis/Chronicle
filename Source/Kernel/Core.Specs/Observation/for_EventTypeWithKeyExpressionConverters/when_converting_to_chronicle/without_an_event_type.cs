// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_EventTypeWithKeyExpressionConverters.when_converting_to_chronicle;

public class without_an_event_type : Specification
{
    Contracts.Observation.EventTypeWithKeyExpression _eventTypeWithKeyExpression;
    Exception _error;

    void Establish() => _eventTypeWithKeyExpression = new() { EventType = null!, Key = "$eventSourceId" };

    void Because() => _error = Catch.Exception(() => _eventTypeWithKeyExpression.ToChronicle());

    [Fact] void should_fail_with_missing_event_type() => _error.ShouldBeOfExactType<MissingEventTypeForKeyExpression>();
}
