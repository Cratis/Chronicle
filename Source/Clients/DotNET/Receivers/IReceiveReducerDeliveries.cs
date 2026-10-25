// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Dispatches received batches through a store's existing reducer dispatcher.
/// </summary>
internal interface IReceiveReducerDeliveries
{
    /// <summary>
    /// Delivers an operation to an active discovered reducer.
    /// </summary>
    /// <param name="id">The reducer identifier.</param>
    /// <param name="operation">The operation.</param>
    /// <returns>The published result, or null if none was published.</returns>
    Task<ReducerResult?> Deliver(ReducerId id, ReduceOperationMessage operation);
}
