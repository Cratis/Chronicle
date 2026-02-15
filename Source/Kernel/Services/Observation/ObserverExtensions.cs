// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Helper methods for working with observers.
/// </summary>
internal static class ObserverExtensions
{
    /// <summary>
    /// Get the observer for a specific command.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
    /// <param name="command"><see cref="IObserverCommand"/> to get the observer for.</param>
    /// <returns>An instance of the observer.</returns>
    public static IObserver GetObserver(this IGrainFactory grainFactory, IObserverCommand command)
    {
        var eventSequenceId = string.IsNullOrEmpty(command.EventSequenceId) ? EventSequenceId.Log : (EventSequenceId)command.EventSequenceId;
        var key = new ObserverKey(command.ObserverId, command.EventStore, command.Namespace, eventSequenceId);
        return grainFactory.GetGrain<IObserver>(key);
    }
}
