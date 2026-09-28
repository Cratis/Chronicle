// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf;

namespace Cratis.Chronicle.Server.Contracts.for_WaitForObserverCompletionRequest.when_round_tripping_through_protobuf;

public class with_a_first_number_at_zero : Specification
{
    WaitForObserverCompletionRequest _result;

    void Because() => _result = Serializer.DeepClone(new WaitForObserverCompletionRequest
    {
        EventStore = "store",
        Namespace = "namespace",
        EventSequenceId = "sequence",
        FirstEventSequenceNumber = 0UL,
        HasFirstEventSequenceNumber = true,
        TailEventSequenceNumber = 1UL
    });

    [Fact] void should_preserve_the_presence_of_zero() => _result.HasFirstEventSequenceNumber.ShouldBeTrue();
    [Fact] void should_preserve_the_first_number() => _result.FirstEventSequenceNumber.ShouldEqual(0UL);
}
