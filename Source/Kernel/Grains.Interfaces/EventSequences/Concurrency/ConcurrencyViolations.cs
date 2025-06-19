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
    : ReadOnlyDictionary<EventSourceId, ConcurrencyViolation>(violations);