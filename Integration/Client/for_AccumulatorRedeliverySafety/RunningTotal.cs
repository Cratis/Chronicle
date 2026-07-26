// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_AccumulatorRedeliverySafety;

/// <summary>
/// Read model built by the running-total reducer.
/// </summary>
/// <param name="Id">The event source the total belongs to.</param>
/// <param name="Total">The running total.</param>
public record RunningTotal(string Id, int Total);
