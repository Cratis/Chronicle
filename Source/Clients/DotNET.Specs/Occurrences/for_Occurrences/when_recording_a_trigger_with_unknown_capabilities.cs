// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences;

public class when_recording_a_trigger_with_unknown_capabilities : an_occurrence_client
{
    void Establish() => ((IKernelCapabilities)_connection).CapabilitiesAreKnown.Returns(false);

    async Task Because() => _error = await Catch.Exception(() => _occurrences.Record(new TestTrigger("hello"), "request-1"));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<OccurrencesNotSupported>();
    [Fact] void should_send_nothing() => _service.DidNotReceive().RecordOccurrence(Arg.Any<RecordOccurrenceRequest>(), Arg.Any<CallContext>());
}
