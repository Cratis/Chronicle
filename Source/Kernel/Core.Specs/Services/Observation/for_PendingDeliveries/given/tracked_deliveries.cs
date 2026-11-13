// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_PendingDeliveries.given;

public class tracked_deliveries : Specification
{
    private protected readonly PendingDeliveries<ObserverSubscriberResult> _pending = new();
    protected readonly TaskCompletionSource<ObserverSubscriberResult> _completion = new();

    protected bool HasEntryCount(string field, int count) => EntryCount(field) == count;

    int EntryCount(string field)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var type = _pending.GetType();
        lock ((Lock)type.GetField("_lock", flags)!.GetValue(_pending)!)
        {
            return ((IDictionary)type.GetField(field, flags)!.GetValue(_pending)!).Count;
        }
    }
}
