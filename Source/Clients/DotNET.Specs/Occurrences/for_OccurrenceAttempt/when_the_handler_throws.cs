// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt;

public class when_the_handler_throws : given.an_occurrence_client
{
    void Establish() => _handler.OnHandle = (_, _, _) => throw new InvalidOperationException("handler failed");

    Task Because() => CreateAttempt(TimeProvider.System).Run(_claim, CancellationToken.None);

    [Fact] void should_fail_the_fenced_claim() => _service.Received(1).FailOccurrence(Arg.Is<FailOccurrenceRequest>(request => request.OccurrenceId == "occurrence" && request.Epoch == 3 && request.Error.Contains("handler failed")), Arg.Any<CallContext>());
    [Fact] void should_not_complete_the_claim() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_dispose_the_handler() => _handler.Disposed.ShouldBeTrue();
}
