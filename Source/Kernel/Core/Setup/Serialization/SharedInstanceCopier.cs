// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Orleans.Serialization.Cloning;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Hands the caller the instance itself, instead of copying it through JSON, for types that are never changed once made.
/// </summary>
/// <remarks>
/// Orleans copies every argument and response of a call within a silo so neither side can change what the other holds. The
/// types handled here are immutable records, or - for a <see cref="JobState"/> - a state read from storage for the one call
/// and not kept by the grain afterwards, so there is nothing to protect. Copying them anyway cost a JSON round trip per call,
/// and for the observer's subscription, which every job step asks for when it activates, thousands of identical copies of its
/// event types - or, for a job state that carries the plan of thousands of steps, megabytes each. Between silos they are still
/// serialized.
/// </remarks>
public sealed class SharedInstanceCopier : IGeneralizedCopier
{
    /// <inheritdoc/>
    public bool IsSupportedType(Type type) =>
        type == typeof(EventType) ||
        type == typeof(ObserverSubscription) ||
        type == typeof(ObserverReaderScope) ||
        typeof(JobState).IsAssignableFrom(type);

    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context) => input;
}
