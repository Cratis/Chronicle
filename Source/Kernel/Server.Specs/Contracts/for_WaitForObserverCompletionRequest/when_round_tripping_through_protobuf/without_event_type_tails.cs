// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf;

namespace Cratis.Chronicle.Server.Contracts.for_WaitForObserverCompletionRequest.when_round_tripping_through_protobuf;

public class without_event_type_tails : Specification
{
    WaitForObserverCompletionRequest _result;

    void Because() => _result = Serializer.DeepClone(new WaitForObserverCompletionRequest
    {
        EventStore = "store",
        Namespace = "namespace",
        EventSequenceId = "sequence",
        TailEventSequenceNumber = 13,
    });

    [Fact] void should_preserve_an_empty_tail_collection() => _result.EventTypeTails.ShouldBeEmpty();
    [Fact] void should_preserve_the_legacy_sequence_number() => _result.TailEventSequenceNumber.ShouldEqual(13ul);
}
