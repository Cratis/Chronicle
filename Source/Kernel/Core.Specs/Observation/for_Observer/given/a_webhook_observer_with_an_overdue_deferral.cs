// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// A webhook observer with an overdue deferral whose recovery jobs cannot be found again, as when listing jobs fails.
/// </summary>
public class a_webhook_observer_with_an_overdue_deferral : a_webhook_observer
{
    protected FailedPartition _failure;
    protected PartitionDeferral _deferral;
    protected int _startCalls;
    protected Func<Result<JobId, StartJobError>?> _startResult = () => Result<JobId, StartJobError>.Success(JobId.New());

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _deferral = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, now.AddMinutes(-2), now.AddMinutes(-1), new(42));
        _failure = _failedPartitionsState.Defer("partition", _deferral);
        _jobsManager
            .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
            .Returns(_ =>
            {
                _startCalls++;
                return Task.FromResult(_startResult()!);
            });
    }

    protected Task Remind() => _observer.ReceiveReminder(Observer.PartitionReminderName(_failure.Id), default);
}
