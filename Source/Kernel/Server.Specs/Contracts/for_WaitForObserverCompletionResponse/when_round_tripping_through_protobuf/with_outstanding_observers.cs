// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf;

namespace Cratis.Chronicle.Server.Contracts.for_WaitForObserverCompletionResponse.when_round_tripping_through_protobuf;

public class with_outstanding_observers : Specification
{
    WaitForObserverCompletionResponse _response;
    WaitForObserverCompletionResponse _result;

    void Establish() => _response = new()
    {
        TimedOut = true,
        OutstandingObservers = new List<string> { "first", "second" },
    };

    void Because() => _result = Serializer.DeepClone(_response);

    [Fact] void should_preserve_the_timeout() => _result.TimedOut.ShouldBeTrue();
    [Fact] void should_preserve_both_outstanding_observers() => _result.OutstandingObservers.ShouldContainOnly("first", "second");
}
