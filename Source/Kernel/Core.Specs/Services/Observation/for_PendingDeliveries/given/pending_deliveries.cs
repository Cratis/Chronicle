// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_PendingDeliveries.given;

public class pending_deliveries : Specification
{
    private protected readonly PendingDeliveries<ObserverSubscriberResult> _pending = new();
    protected readonly TaskCompletionSource<ObserverSubscriberResult> _first = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource<ObserverSubscriberResult> _second = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource<ObserverSubscriberResult> _other = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected ulong _firstId;
    protected ulong _secondId;

    void Establish()
    {
        _firstId = _pending.Add("partition", _first);
        _secondId = _pending.Add("partition", _second);
        _pending.Add("other", _other);
    }
}
