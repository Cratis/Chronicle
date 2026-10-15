// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Applies caller-supplied scope correctness checks without changing strategy-issued policy.
/// </summary>
internal static class ExplicitConcurrencyScope
{
    /// <summary>
    /// Completes narrowed caller expectations, leaving empty builders unchecked.
    /// </summary>
    /// <param name="scope">The supplied scope.</param>
    /// <param name="options">The compatibility switches.</param>
    /// <param name="logger">The append diagnostic logger.</param>
    /// <returns>The scope to validate.</returns>
    internal static ConcurrencyScope Normalize(ConcurrencyScope scope, ConcurrencyOptions options, ILogger<EventSequence> logger)
    {
        if (options.SkipIncompleteExplicitScopes || scope.IssuedByStrategy || !scope.IsIncomplete) return scope;
        var predicate = ReadProtectionPredicate.Normalize(scope);
        if (predicate.EventSourceId is not null || predicate.EventSourceType is not null || predicate.EventStreamType is not null || predicate.EventStreamId is not null || predicate.EventTypes!.Any())
        {
            return scope with { SequenceNumber = EventSequenceNumber.BeforeFirst };
        }
        logger.IncompleteScopeWithoutNarrowing();
        return ConcurrencyScope.None;
    }

    /// <summary>
    /// Refuses a linked stream mismatch for a single-route append.
    /// </summary>
    /// <param name="scope">The explicit scope.</param>
    /// <param name="routing">The append route, absent for independent conditions.</param>
    /// <param name="options">The compatibility switches.</param>
    /// <exception cref="ConcurrencyScopeStreamDoesNotMatchAppend">The streams differ on the same definition.</exception>
    internal static void ValidateRoute(ConcurrencyScope? scope, ResolvedEventRouting? routing, ConcurrencyOptions options)
    {
        if (options.AllowRouteScopeStreamMismatch || scope?.DefinitionType is null || routing is null || scope.DefinitionType != routing.Definition.ClrType) return;
        var stream = ReadProtectionPredicate.Normalize(scope).EventStreamType;
        if (stream is not null && stream != routing.StreamType)
        {
            throw new ConcurrencyScopeStreamDoesNotMatchAppend(routing.Definition.ClrType, stream, routing.StreamType);
        }
    }
}
