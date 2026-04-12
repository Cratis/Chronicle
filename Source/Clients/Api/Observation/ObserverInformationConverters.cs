// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.Events;

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Converts <see cref="Contracts.Observation.ObserverInformationResponse"/> to an API representation.
/// </summary>
internal static class ObserverInformationConverters
{
    /// <summary>
    /// Converts the <see cref="Contracts.Observation.ObserverInformationResponse"/> to an API representation.
    /// </summary>
    /// <param name="observerInformation">The observer information.</param>
    /// <returns>The API representation of the observer information.</returns>
    public static ObserverInformation ToApi(this Contracts.Observation.ObserverInformationResponse observerInformation) =>
        new(
            observerInformation.Id,
            observerInformation.EventSequenceId,
            (ObserverType)(int)observerInformation.Type,
            (ObserverOwner)(int)observerInformation.Owner,
            (observerInformation.EventTypes ?? []).Select(id => new EventType(id, 1, false)),
            observerInformation.NextEventSequenceNumber,
            observerInformation.LastHandledEventSequenceNumber,
            (ObserverRunningState)(int)observerInformation.RunningState,
            observerInformation.IsSubscribed,
            observerInformation.IsReplayable);

    /// <summary>
    /// Converts a collection of <see cref="Contracts.Observation.ObserverInformationResponse"/> to an API representation.
    /// </summary>
    /// <param name="observerInformation">The collection of observer information.</param>
    /// <returns>The API representation of the collection of observer information.</returns>
    public static IEnumerable<ObserverInformation> ToApi(this IEnumerable<Contracts.Observation.ObserverInformationResponse> observerInformation) =>
        observerInformation.Select(ToApi);
}
