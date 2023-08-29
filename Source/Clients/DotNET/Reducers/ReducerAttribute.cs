// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation.Reducers;

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
    public ReducerAttribute(string reducerIdAsString)
    {
        ReducerId = reducerIdAsString;
    }

    /// <summary>
    /// Gets the unique identifier for the reducer.
    /// </summary>
    public ReducerId ReducerId { get; }

    /// <summary>
    /// Gets the unique identifier for an event sequence.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = EventSequenceId.Log;
}
