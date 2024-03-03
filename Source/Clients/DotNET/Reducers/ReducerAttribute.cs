// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Attribute used to adorn classes to identify a reducer uniquely. The reducer also needs to implement <see cref="IReducerFor{TReadModel}"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ReducerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="ReducerAttribute"/>.
    /// </summary>
    /// <param name="reducerIdAsString"><see cref="ReducerId"/> represented as string. Must be a valid Guid.</param>
    /// <param name="eventSequence">Optional the name of the event sequence to observe, this will take precedence over inbox.</param>
    /// <param name="isActive">Optional whether or not the reducer is active or not. If its passive, it won't run actively on the Kernel.</param>
    public ReducerAttribute(string reducerIdAsString, string? eventSequence = default, bool isActive = true)
    {
        ReducerId = reducerIdAsString;
        EventSequenceId = eventSequence ?? EventSequenceId.Log;
        IsActive = isActive;
    }

    /// <summary>
    /// Gets the unique identifier for the reducer.
    /// </summary>
    public ReducerId ReducerId { get; }

    /// <summary>
    /// Gets the unique identifier for an event sequence.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = EventSequenceId.Log;

    /// <summary>
    /// Gets whether or not the reducer should be actively running on the Kernel.
    /// </summary>
    public bool IsActive { get; } = true;
}
