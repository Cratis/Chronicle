// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Describes a <see cref="ConcurrencyScope"/> in words for developer-facing messages, without sentinel sequence numbers.
/// </summary>
internal static class ConcurrencyScopeDescription
{
    /// <summary>
    /// Describes what a scope expects, completing the phrase "a scope that ...".
    /// </summary>
    /// <param name="scope">The scope to describe.</param>
    /// <returns>The description.</returns>
    public static string Describe(ConcurrencyScope scope)
    {
        if (scope == ConcurrencyScope.None)
        {
            return "turns the concurrency check off";
        }

        var expectation = "has no expected position";
        if (scope.SequenceNumber.IsBeforeFirst)
        {
            expectation = "expects no events yet";
        }
        else if (scope.SequenceNumber.IsActualValue)
        {
            expectation = $"expects no events after sequence number {scope.SequenceNumber.Value}";
        }

        var filters = new List<string>();
        if (scope.EventTypes?.Any() == true)
        {
            filters.Add($"event types {string.Join(", ", scope.EventTypes.Select(_ => _.Id.Value))}");
        }

        if (scope.EventStreamType is not null && scope.EventStreamType != EventStreamType.All)
        {
            filters.Add($"event stream type '{scope.EventStreamType}'");
        }

        if (scope.EventStreamId is not null && scope.EventStreamId != EventStreamId.Default)
        {
            filters.Add($"event stream '{scope.EventStreamId}'");
        }

        if (scope.EventSourceType is not null && scope.EventSourceType != EventSourceType.Default)
        {
            filters.Add($"event source type '{scope.EventSourceType}'");
        }

        return filters.Count == 0 ? expectation : $"{expectation} ({string.Join("; ", filters)})";
    }
}
