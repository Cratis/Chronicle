// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Optional attribute used to adorn classes to configure a reducer. The reducer will have to implement <see cref="IReducerFor{TReadModel}"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ReducerAttribute"/>.
/// </remarks>
/// <param name="id">Optional identifier. If not specified, it will default to the fully qualified type name.</param>
/// <param name="eventSequence">Optional the name of the event sequence to observe. Defaults to the event log.</param>
/// <param name="isActive">Optional whether or not the reducer is active or not. If its passive, it won't run actively on the Kernel.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ReducerAttribute(string id = "", string? eventSequence = default, bool isActive = true) : Attribute
{
    /// <summary>
    /// Gets the unique identifier for the reducer.
    /// </summary>
    public ReducerId Id { get; } = id;

    /// <summary>
    /// Gets the unique identifier for an event sequence.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = eventSequence ?? EventSequenceId.Log;

    /// <summary>
    /// Gets whether or not the reducer should be actively running on the Kernel.
    /// </summary>
    public bool IsActive { get; } = isActive;
}
