// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt;

public class when_renewing_a_running_handler : an_occurrence_client
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _renewed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ManualOccurrenceClock _clock = new();
    ManualOccurrenceClock.ManualTimer _timer;

    void Establish()
    {
        _handler.OnHandle = (_, _, token) => _release.Task.WaitAsync(token);
        _service.RenewOccurrenceLease(Arg.Any<RenewOccurrenceLeaseRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            _renewed.TrySetResult();
            return CommandResult<CompleteOccurrenceResponse>.Success(Guid.NewGuid(), new());
        });
    }

    async Task Because()
    {
        var running = CreateAttempt(_clock).Run(_claim, _clock.GetTimestamp(), CancellationToken.None);
        _timer = await _clock.WaitForTimer(TimeSpan.FromSeconds(20));
        _timer.Tick();
        await _renewed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _release.SetResult();
        await running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_renew_before_the_requested_lease_expires() => (_timer.DueTime < TimeSpan.FromSeconds(OccurrenceAttempt.LeaseSeconds)).ShouldBeTrue();
    [Fact] void should_renew_the_current_epoch() => _service.Received(1).RenewOccurrenceLease(Arg.Is<RenewOccurrenceLeaseRequest>(request => request.Epoch == 3 && request.OccurrenceId == "occurrence" && request.LeaseSeconds == 60), Arg.Any<CallContext>());
    [Fact] void should_stop_the_renewal_timer() => _timer.Disposed.ShouldBeTrue();
}
