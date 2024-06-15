// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Defines the endpoint called for receiving events from the kernel.
/// </summary>
public interface IClientReducers
{
    /// <summary>
    /// Called for events to be handled.
    /// </summary>
    /// <param name="reducerId">The <see cref="ReducerId"/> of the reducer it is for.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/>.</param>
    /// <param name="initialAsJson">The initial state.</param>
    /// <returns>Reduced result.</returns>
    Task<InternalReduceResult> OnNext(ReducerId reducerId, IEnumerable<AppendedEvent> events, JsonObject? initialAsJson);
}
