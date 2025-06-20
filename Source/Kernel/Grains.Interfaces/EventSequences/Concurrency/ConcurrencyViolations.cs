// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences.Concurrency;

/// <summary>
/// Represents concurrency violations.
/// </summary>
/// <param name="violations">The <see cref="ConcurrencyViolation"/> violations.</param>
public class ConcurrencyViolations(IDictionary<EventSourceId, ConcurrencyViolation> violations)
    : ReadOnlyDictionary<EventSourceId, ConcurrencyViolation>(violations)
{
    /// <summary>
    /// Gets an empty <see cref="ConcurrencyViolations"/>.
    /// </summary>
    public static readonly ConcurrencyViolations None = new(new Dictionary<EventSourceId, ConcurrencyViolation>());

    /// <summary>
    /// Gets a value indicating whether there are any <see cref="ConcurrencyViolation"/>.
    /// </summary>
    public bool HasViolations => Count > 0;
}