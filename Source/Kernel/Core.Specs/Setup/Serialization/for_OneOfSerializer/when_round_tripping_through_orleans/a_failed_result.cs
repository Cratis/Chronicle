// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Setup.Serialization.for_OneOfSerializer.when_round_tripping_through_orleans;

public class a_failed_result : given.a_configured_orleans_serializer
{
    Result<EventSequenceNumber, GetSequenceNumberError> _original;
    Result<EventSequenceNumber, GetSequenceNumberError> _result;

    void Establish() => _original = GetSequenceNumberError.NotFound;

    void Because() => _result = RoundTrip(_original);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_round_trip_the_error() => _result.AsT1.ShouldEqual(_original.AsT1);
}
