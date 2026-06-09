// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Setup.Serialization.for_OneOfSerializer.when_round_tripping_through_orleans;

public class a_successful_result : given.a_configured_orleans_serializer
{
    Result<EventSequenceNumber, GetSequenceNumberError> _original;
    Result<EventSequenceNumber, GetSequenceNumberError> _result;

    void Establish() => _original = (EventSequenceNumber)42UL;

    void Because() => _result = RoundTrip(_original);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_round_trip_the_value() => _result.AsT0.ShouldEqual(_original.AsT0);
}
