// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Traces;

namespace Cratis.Chronicle.EventSequences;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class EventSequenceTraces
{
    [Span("client.event_sequence.append", ActivityKind.Client)]
    internal static partial IActivityScope<EventSequence> Append(
        this IActivitySource<EventSequence> source,
        string eventStoreName,
        string namespaceName,
        string eventSequenceId,
        string eventSourceType,
        string eventSourceId);

    [Span("client.event_sequence.append_many", ActivityKind.Client)]
    internal static partial IActivityScope<EventSequence> AppendMany(
        this IActivitySource<EventSequence> source,
        string eventStoreName,
        string namespaceName,
        string eventSequenceId);
}
