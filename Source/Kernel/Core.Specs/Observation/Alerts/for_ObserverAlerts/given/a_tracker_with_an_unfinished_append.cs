// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.given;

public class a_tracker_with_an_unfinished_append : an_alert_tracker
{
    protected readonly TaskCompletionSource _drainRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _appendFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected ObserverAlertReceipt _uncertain;
    protected bool _acknowledgedBeforeCommit;
    object _pendingTransition;

    async Task Establish()
    {
        AppendUsing(() =>
        {
            _pendingTransition = _serialized;
            throw new TimeoutException("Append still executing on the sequence");
        });
        _uncertain = await _tracker.Reconcile(_snapshot);
        AppendSucceedsFrom(1);
        _sequence.DrainAppends().Returns(_ =>
        {
            _drainRequested.TrySetResult();
            return _appendFinished.Task;
        });
    }

    protected void CompleteAppend()
    {
        RecordDurable(_pendingTransition);
        _appendFinished.TrySetResult();
    }

    protected async Task ReconcileBeforeLateCommit()
    {
        var report = _tracker.Reconcile(_snapshot);
        await _drainRequested.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _acknowledgedBeforeCommit = report.IsCompleted;
        CompleteAppend();
        _receipt = await report.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }
}
