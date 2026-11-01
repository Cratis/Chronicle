// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_getting_a_cancelled_page : given.a_paged_event_sequence
{
    Exception? _exception;

    async Task Because() => _exception = await Catch.Exception(async () => await _eventSequence.GetPage(EventSequencePageRequest.Forward(null, 1), new CancellationToken(true)));

    [Fact] void should_honor_cancellation_before_sending_a_query() => _exception.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_send_a_query() => _sequences.DidNotReceive().Page(Arg.Any<PageRequest>(), Arg.Any<CallContext>());
}
