// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Defines a system that can validate event concurrency.
/// </summary>
public interface IConcurrencyValidator
{
    /// <summary>
    /// Validates a single <see cref="ConcurrencyScope"/>.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/>.</param>
    /// <param name="scope">The <see cref="ConcurrencyScope"/>.</param>
    /// <returns><see cref="Option{TValue}"/> of <see cref="ConcurrencyViolation"/>.</returns>
    /// <remarks>
    /// A scope with <see cref="ConcurrencyScope.ExpectsNoMatchingEvent"/> is violated by the existence of any event
    /// matching its narrowing, not by the tail having moved past a number. That is the check the first append into
    /// a scope needs, and the only one it can express.
    /// </remarks>
    ValueTask<Option<ConcurrencyViolation>> Validate(EventSourceId eventSourceId, ConcurrencyScope scope);

    /// <summary>
    /// Validates multiple <see cref="ConcurrencyScopes"/>.
    /// </summary>
    /// <param name="scopes">The <see cref="ConcurrencyScopes"/>.</param>
    /// <returns>Collection of <see cref="ConcurrencyViolation"/>.</returns>
    ValueTask<IEnumerable<ConcurrencyViolation>> Validate(ConcurrencyScopes scopes);
}
