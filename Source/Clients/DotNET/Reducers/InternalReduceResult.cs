// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Represents the result of a reduce operation.
/// </summary>
/// <param name="State">Potential state, unless errored.</param>
/// <param name="LastSuccessfullyObservedEvent">The sequence number of the last successfully observed event.</param>
/// <param name="Error">Potential error that occurred.</param>
public record InternalReduceResult(object? State, EventSequenceNumber LastSuccessfullyObservedEvent, Exception? Error = default)
{
    /// <summary>
    /// Gets whether or not the reduce operation was successful.
    /// </summary>
    public bool IsSuccess => Error is null;
}
