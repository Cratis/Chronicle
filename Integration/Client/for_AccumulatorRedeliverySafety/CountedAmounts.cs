// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_AccumulatorRedeliverySafety;

/// <summary>
/// Read model built by an event-source-keyed counting projection.
/// </summary>
/// <param name="Id">The event source the counts belong to.</param>
/// <param name="Handled">The number of events counted.</param>
public record CountedAmounts(string Id, int Handled);
