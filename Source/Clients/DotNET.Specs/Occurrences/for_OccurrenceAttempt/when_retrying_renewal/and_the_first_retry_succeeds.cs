// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_retrying_renewal;

public class and_the_first_retry_succeeds : given.a_running_attempt
{
    bool _retried;

    void Establish() => _renew = count => count == 1 ? Unavailable() : Task.FromResult(Renewed());

    async Task Because()
    {
        await RenewAtTwentySeconds();
        _retried = await AdvanceRetry();
        if (_retried)
        {
            _clock.Advance(TimeSpan.FromSeconds(20));
            await NextRenewal();
        }
        await Finish();
    }

    [Fact] void should_retry_after_one_second() => _retried.ShouldBeTrue();
    [Fact] void should_resume_the_normal_renewal_interval() => _renewalTimes.ShouldEqual([TimeSpan.FromSeconds(20).Ticks, TimeSpan.FromSeconds(21).Ticks, TimeSpan.FromSeconds(41).Ticks]);
    [Fact] void should_keep_the_claim_epoch_and_requested_lease() => _service.Received(3).RenewOccurrenceLease(Arg.Is<RenewOccurrenceLeaseRequest>(request => request.Epoch == 3 && request.OccurrenceId == "occurrence" && request.LeaseSeconds == 60), Arg.Any<CallContext>());
    [Fact] void should_complete_the_handler() => _service.Received(1).CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
}
