// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_AccumulatorRedeliverySafety;

/// <summary>
/// Read model built by a projection whose key collapses every event source onto one document.
/// </summary>
/// <param name="Id">The constant key.</param>
/// <param name="Handled">The number of events counted.</param>
public record CollapsedCount(string Id, int Handled);
