// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt;

public class when_completion_acknowledgement_is_unknown : given.an_occurrence_client
{
    void Establish() => _service.CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>()).Returns(Task.FromException<CommandResult<CompleteOccurrenceResponse>>(new IOException("reply lost")));

    async Task Because() => _error = await Catch.Exception(() => CreateAttempt(TimeProvider.System).Run(_claim, CancellationToken.None));

    [Fact] void should_not_report_success() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_not_classify_transport_failure_as_handler_failure() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
