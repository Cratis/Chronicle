// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Records what the seat reactors were called with.
/// </summary>
public class SeatBatchLog
{
    /// <summary>
    /// Gets the events of each batch method call, in call order.
    /// </summary>
    public List<IReadOnlyList<object>> Batches { get; } = [];

    /// <summary>
    /// Gets the <see cref="BatchContext"/> of each batch method call, in call order.
    /// </summary>
    public List<BatchContext> Contexts { get; } = [];

    /// <summary>
    /// Gets the events the per-event method was called with, in call order.
    /// </summary>
    public List<object> PerEvent { get; } = [];
}
