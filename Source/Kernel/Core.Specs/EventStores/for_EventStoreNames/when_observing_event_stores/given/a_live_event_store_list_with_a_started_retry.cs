// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_observing_event_stores.given;

public class a_live_event_store_list_with_a_started_retry : a_live_event_store_list
{
    protected started_retry_scheduler _startedRetries;

    protected override IScheduler RetryScheduler => _startedRetries;

    void Establish()
    {
        _startedRetries = new();
        _initialization.EnsureAll(Arg.Is<IEnumerable<EventStoreName>>(stores => stores.SequenceEqual(_stores))).Returns([_stores[0]]);
    }
}
