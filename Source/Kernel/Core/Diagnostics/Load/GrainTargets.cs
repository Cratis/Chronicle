// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;

namespace Cratis.Chronicle.Diagnostics.Load;

/// <summary>
/// Names the grain a call is made to for the load report, without recording a grain key.
/// </summary>
/// <remarks>
/// Only the grain interface and the event store go into the report. The event store is read from the key of the grains
/// whose key is known to carry it: the managers are keyed by the event store itself, and the observers, event sequences,
/// projections and webhooks by their identifier followed by the event store. Any other grain is reported without one,
/// rather than guessing at a key's shape.
/// </remarks>
static class GrainTargets
{
    const char KeySeparator = '#';

    static readonly FrozenDictionary<string, int> _eventStoreSegments = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["IProjectionsManager"] = 0,
        ["IReadModelsManager"] = 0,
        ["ICapturesManager"] = 0,
        ["IObserver"] = 1,
        ["IEventSequence"] = 1,
        ["IProjection"] = 1,
        ["IWebhook"] = 1
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Gets the name of the grain interface a method belongs to.
    /// </summary>
    /// <param name="method">The interface method called.</param>
    /// <returns>The interface name without generic arity, or <c language="csharp">unknown</c>.</returns>
    internal static string GrainTypeOf(MethodInfo? method)
    {
        var name = method?.DeclaringType?.Name;
        if (string.IsNullOrEmpty(name))
        {
            return "unknown";
        }

        var arity = name.IndexOf('`', StringComparison.Ordinal);
        return arity < 0 ? name : name[..arity];
    }

    /// <summary>
    /// Gets the event store a grain belongs to.
    /// </summary>
    /// <param name="grainType">The name of the grain interface.</param>
    /// <param name="key">The grain's key.</param>
    /// <returns>The event store, or an empty string when the grain's key is not known to carry one.</returns>
    internal static string EventStoreOf(string grainType, string key)
    {
        if (!_eventStoreSegments.TryGetValue(grainType, out var segment))
        {
            return string.Empty;
        }

        var span = key.AsSpan();
        for (var skipped = 0; skipped < segment; skipped++)
        {
            var separator = span.IndexOf(KeySeparator);
            if (separator < 0)
            {
                return string.Empty;
            }

            span = span[(separator + 1)..];
        }

        var end = span.IndexOf(KeySeparator);
        return (end < 0 ? span : span[..end]).ToString();
    }
}
