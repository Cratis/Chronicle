// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Defines an invoker of a reducer that can tell a batch method about the page it reduces a partition of.
/// </summary>
internal interface IBatchReducerInvoker
{
    /// <summary>
    /// Gets a value indicating whether the reducer reduces with a batch method rather than per-event methods.
    /// </summary>
    bool HasBatchMethod { get; }

    /// <summary>
    /// Reduces the events of one partition.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> for the reduction.</param>
    /// <param name="eventsAndContexts">The events of the partition, in order.</param>
    /// <param name="initialReadModelContent">The current read model state.</param>
    /// <param name="context">The <see cref="BatchContext"/> of the page.</param>
    /// <returns>The <see cref="ReduceResult"/>.</returns>
    Task<ReduceResult> Invoke(IServiceProvider serviceProvider, IEnumerable<EventAndContext> eventsAndContexts, object? initialReadModelContent, BatchContext context);
}
