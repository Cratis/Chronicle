// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_retrying_renewal;

public class and_the_second_retry_succeeds : given.a_running_attempt
{
    bool _retriedTwice;

    void Establish() => _renew = count => count < 3 ? Unavailable() : Task.FromResult(Renewed());

    async Task Because()
    {
        await RenewAtTwentySeconds();
        _retriedTwice = await AdvanceRetry() && await AdvanceRetry();
        await Finish();
    }

    [Fact] void should_allow_two_additional_attempts() => _retriedTwice.ShouldBeTrue();
    [Fact] void should_space_each_retry_by_one_second() => _renewalTimes.ShouldEqual([TimeSpan.FromSeconds(20).Ticks, TimeSpan.FromSeconds(21).Ticks, TimeSpan.FromSeconds(22).Ticks]);
    [Fact] void should_renew_only_the_current_epoch() => _service.Received(3).RenewOccurrenceLease(Arg.Is<RenewOccurrenceLeaseRequest>(request => request.Epoch == 3 && request.LeaseSeconds == 60), Arg.Any<CallContext>());
    [Fact] void should_complete_the_handler() => _service.Received(1).CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
}
