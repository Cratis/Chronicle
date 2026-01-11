// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Converts between MongoDB and kernel representations of observer states.
/// </summary>
public static class ObserverStateConverters
{
    /// <summary>
    /// Converts a kernel representation of an observer state to its MongoDB representation.
    /// </summary>
    /// <param name="state">The kernel observer state.</param>
    /// <returns>The MongoDB representation of the observer state.</returns>
    public static ObserverState ToMongoDB(this Chronicle.Storage.Observation.ObserverState state) =>
        new()
        {
            Id = state.Identifier,
            LastHandledEventSequenceNumber = state.LastHandledEventSequenceNumber,
            NextEventSequenceNumber = state.NextEventSequenceNumber,
            RunningState = state.RunningState,
            ReplayingPartitions = state.ReplayingPartitions,
            CatchingUpPartitions = state.CatchingUpPartitions,
            IsReplaying = state.IsReplaying,
            SubscribesToAllEvents = state.SubscribesToAllEvents
        };

    /// <summary>
    /// Converts a MongoDB representation of an observer state to its kernel representation.
    /// </summary>
    /// <param name="state">The MongoDB observer state.</param>
    /// <returns>The kernel representation of the observer state.</returns>
    public static Chronicle.Storage.Observation.ObserverState ToKernel(this ObserverState state) =>
        new(
            state.Id,
            state.LastHandledEventSequenceNumber,
            state.RunningState,
            state.ReplayingPartitions.ToHashSet(),
            state.CatchingUpPartitions.ToHashSet(),
            [],
            state.IsReplaying,
            state.SubscribesToAllEvents)
        {
            NextEventSequenceNumber = state.NextEventSequenceNumber
        };

    /// <summary>
    /// Converts a MongoDB representation of an observer state with failed partitions to its kernel representation.
    /// </summary>
    /// <param name="state">The MongoDB observer state.</param>
    /// <returns>The kernel representation of the observer state.</returns>
    public static Chronicle.Storage.Observation.ObserverState ToKernel(this ObserverStateWithFailedPartitions state) =>
        new(
            state.Id,
            state.LastHandledEventSequenceNumber,
            state.RunningState,
            state.ReplayingPartitions.ToHashSet(),
            state.CatchingUpPartitions.ToHashSet(),
            state.FailedPartitions,
            state.IsReplaying,
            state.SubscribesToAllEvents)
        {
            NextEventSequenceNumber = state.NextEventSequenceNumber
        };

    /// <summary>
    /// Converts a collection of MongoDB representations of observer states to their kernel representations.
    /// </summary>
    /// <param name="states">The MongoDB observer states.</param>
    /// <returns>The kernel representations of the observer states.</returns>
    public static IEnumerable<Chronicle.Storage.Observation.ObserverState> ToKernel(this IEnumerable<ObserverState> states) =>
        states.Select(ToKernel).ToArray();
}
