// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Tracing helpers for event sequence operations.
/// </summary>
internal static class Tracing
{
    /// <summary>
    /// Starts tracing for an append operation.
    /// </summary>
    /// <param name="eventStoreName">The <see cref="EventStoreName"/>.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/>.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/>.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/>.</param>
    /// <returns>The started <see cref="Activity"/>.</returns>
    public static Activity? Append(
        EventStoreName eventStoreName,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        EventSourceType eventSourceType,
        EventSourceId eventSourceId)
    {
        var activity = ClientActivity.Source.StartActivity(nameof(Append), ActivityKind.Client);
        activity?.Tag(eventStoreName);
        activity?.Tag(@namespace);
        activity?.Tag(eventSequenceId);
        activity?.Tag(eventSourceType, eventSourceId);
        return activity;
    }

    /// <summary>
    /// Starts tracing for an append-many operation.
    /// </summary>
    /// <param name="eventStoreName">The <see cref="EventStoreName"/>.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/>.</param>
    /// <returns>The started <see cref="Activity"/>.</returns>
    public static Activity? AppendMany(
        EventStoreName eventStoreName,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId)
    {
        var activity = ClientActivity.Source.StartActivity(nameof(AppendMany), ActivityKind.Client);
        activity?.Tag(eventStoreName);
        activity?.Tag(@namespace);
        activity?.Tag(eventSequenceId);
        return activity;
    }
}
