// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt;

public class when_completion_loses_the_lease : given.an_occurrence_client
{
    void Establish() => _service.CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>()).Returns(CommandResult<CompleteOccurrenceResponse>.Success(Guid.NewGuid(), new() { Status = OccurrenceLeaseResult.OccurrenceLeaseLost }));

    async Task Because() => _error = await Catch.Exception(() => CreateAttempt(TimeProvider.System).Run(_claim, CancellationToken.None));

    [Fact] void should_treat_the_attempt_as_done() => _error.ShouldBeNull();
    [Fact] void should_not_fail_a_superseded_claim() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
