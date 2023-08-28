// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Observation.Reducers;

/// <summary>
/// Represents the result of a reduce operation.
/// </summary>
/// <param name="State">Potential state, unless errored.</param>
/// <param name="LastSuccessfullyObservedEvent">The sequence number of the last successfully observed event.</param>
public record ReduceResult(JsonObject? State, EventSequenceNumber LastSuccessfullyObservedEvent);
