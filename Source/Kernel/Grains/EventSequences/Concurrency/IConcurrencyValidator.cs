// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.Grains.EventSequences.Concurrency;

/// <summary>
/// Defines a system that can validate event concurrency.
/// </summary>
public interface IConcurrencyValidator
{
    Task<ConcurrencyValidationResults> Validate(EventSourceId eventSourceId, ConcurrencyScope scope);
}
