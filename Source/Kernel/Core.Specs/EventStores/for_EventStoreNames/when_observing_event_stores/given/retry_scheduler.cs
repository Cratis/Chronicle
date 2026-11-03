// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_observing_event_stores.given;

public class retry_scheduler() : VirtualTimeScheduler<long, long>(0, Comparer<long>.Default)
{
    protected override long Add(long absolute, long relative) => absolute + relative;

    protected override DateTimeOffset ToDateTimeOffset(long absolute) => new(absolute, TimeSpan.Zero);

    protected override long ToRelative(TimeSpan timeSpan) => timeSpan.Ticks;
}
