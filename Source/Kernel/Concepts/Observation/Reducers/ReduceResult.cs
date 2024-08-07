// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.Reducers;

/// <summary>
/// Represents the result of a reduce operation.
/// </summary>
/// <param name="State">Potential state, unless errored.</param>
/// <param name="LastSuccessfullyObservedEvent">The sequence number of the last successfully observed event.</param>
/// <param name="ErrorMessages">Collection of error messages, if any.</param>
/// <param name="StackTrace">The stack trace, if an error occurred.</param>
public record ReduceResult(JsonObject? State, EventSequenceNumber LastSuccessfullyObservedEvent, IEnumerable<string> ErrorMessages, string StackTrace);
