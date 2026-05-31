// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Traces;

namespace Cratis.Chronicle.EventSequences;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class AppendedEventsQueueTraces
{
    [Span("cratis.chronicle.appended_events_queue.enqueue", ActivityKind.Internal)]
    internal static partial IActivityScope<AppendedEventsQueue> Enqueue(this IActivitySource<AppendedEventsQueue> source);

    [Span("cratis.chronicle.appended_events_queue.dispatch", ActivityKind.Internal)]
    internal static partial IActivityScope<AppendedEventsQueue> Dispatch(this IActivitySource<AppendedEventsQueue> source);
}
