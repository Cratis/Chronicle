// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Partial for keeping a catch-up from starting below an event a live delivery is still handing to the subscriber.
/// </summary>
/// <remarks>
/// A live delivery decides to deliver, then awaits the subscriber before it moves the cursor past what it delivered. A
/// catch-up job reporting back interleaves with it and can start the next catch-up in the same turn; that catch-up would read its
/// starting point from the cursor before the live delivery has moved it, and deliver the same event again. So a catch-up
/// waits for live deliveries in flight to settle before it reads where to start.
/// </remarks>
public partial class Observer
{
    static readonly AsyncLocal<bool> _insideLiveDelivery = new();
    int _liveDeliveriesInFlight;
    TaskCompletionSource? _liveDeliveriesSettled;

    LiveDelivery BeginLiveDelivery()
    {
        _liveDeliveriesInFlight++;
        _liveDeliveriesSettled ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
        var outer = _insideLiveDelivery.Value;
        _insideLiveDelivery.Value = true;
        return new(this, outer);
    }

    void EndLiveDelivery(bool outer)
    {
        _insideLiveDelivery.Value = outer;
        if (--_liveDeliveriesInFlight > 0)
        {
            return;
        }

        var settled = _liveDeliveriesSettled;
        _liveDeliveriesSettled = null;
        settled?.TrySetResult();
    }

    async Task LiveDeliveriesSettled()
    {
        // A catch-up started from within a live delivery would wait for itself; the delivery's own cursor update follows it.
        if (_liveDeliveriesInFlight == 0 || _insideLiveDelivery.Value || _liveDeliveriesSettled is not { } settled)
        {
            return;
        }

        logger.WaitingForLiveDeliveriesBeforeCatchUp(_liveDeliveriesInFlight);
        await settled.Task;
    }

    readonly struct LiveDelivery(Observer observer, bool outer) : IDisposable
    {
        public void Dispose() => observer.EndLiveDelivery(outer);
    }
}
