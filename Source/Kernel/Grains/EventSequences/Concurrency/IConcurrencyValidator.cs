// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Monads;

namespace Cratis.Chronicle.Grains.EventSequences.Concurrency;

/// <summary>
/// Defines a system that can validate event concurrency.
/// </summary>
public interface IConcurrencyValidator
{
    Task<Option<ConcurrencyViolation>> Validate(EventSourceId eventSourceId, ConcurrencyScope scope);
    Task<IDictionary<EventSourceId, ConcurrencyViolation>> Validate(IDictionary<EventSourceId, ConcurrencyScope> scopes);
}
