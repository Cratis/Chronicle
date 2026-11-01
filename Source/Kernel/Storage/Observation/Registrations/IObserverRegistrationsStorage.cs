// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Registrations;

namespace Cratis.Chronicle.Storage.Observation.Registrations;

/// <summary>
/// Stores registrations, owner settings and the deployment generation floor for one event store.
/// Reads never create records; missing registrations retain legacy behavior.
/// </summary>
public interface IObserverRegistrationsStorage
{
    /// <summary>
    /// Finds the registration identified by observer and sequence.
    /// </summary>
    /// <param name="observerId">The observer identifier.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <returns>The registration, or null if absent.</returns>
    Task<ObserverRegistration?> Get(ObserverId observerId, EventSequenceId eventSequenceId);

    /// <summary>
    /// Finds all active registrations.
    /// </summary>
    /// <returns>The active registrations.</returns>
    Task<IEnumerable<ObserverRegistration>> GetActive();

    /// <summary>
    /// Finds all registrations belonging to a program, including inactive registrations.
    /// </summary>
    /// <param name="owner">The program identifier.</param>
    /// <returns>The owner's registrations.</returns>
    Task<IEnumerable<ObserverRegistration>> GetByOwner(ProgramIdentifier owner);

    /// <summary>
    /// Atomically saves a registration if its revision is exactly one greater than the expected revision.
    /// </summary>
    /// <param name="registration">The complete registration.</param>
    /// <param name="expectedRevision">The stored revision; zero requires an absent record.</param>
    /// <returns>Whether the write succeeded.</returns>
    Task<bool> TrySave(ObserverRegistration registration, ObserverRegistrationRevision expectedRevision);

    /// <summary>
    /// Deletes a registration only if its revision matches.
    /// </summary>
    /// <param name="observerId">The observer identifier.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <param name="expectedRevision">The stored revision.</param>
    /// <returns>Whether a registration was deleted.</returns>
    Task<bool> TryDelete(ObserverId observerId, EventSequenceId eventSequenceId, ObserverRegistrationRevision expectedRevision);

    /// <summary>
    /// Reads the event-store generation floor without initializing it.
    /// </summary>
    /// <returns>The floor, or null if absent.</returns>
    Task<ObserverGenerationFloor?> GetGenerationFloor();

    /// <summary>
    /// Atomically advances the floor version by one without lowering its generation.
    /// </summary>
    /// <param name="floor">The new floor.</param>
    /// <param name="expectedVersion">The stored version; zero requires an absent record.</param>
    /// <returns>Whether the write succeeded.</returns>
    Task<bool> TrySaveGenerationFloor(ObserverGenerationFloor floor, ulong expectedVersion);

    /// <summary>
    /// Reads a program's event-store-wide HTTP delivery settings.
    /// </summary>
    /// <param name="owner">The program identifier.</param>
    /// <returns>The settings, or null if absent (the default timeout is thirty seconds).</returns>
    Task<ObserverOwnerSettings?> GetOwnerSettings(ProgramIdentifier owner);

    /// <summary>
    /// Atomically advances the owner's settings revision by one, rejecting invalid timeouts.
    /// </summary>
    /// <param name="settings">The owner's settings.</param>
    /// <param name="expectedRevision">The stored settings revision; zero requires an absent record.</param>
    /// <returns>Whether the write succeeded.</returns>
    Task<bool> TrySaveOwnerSettings(ObserverOwnerSettings settings, ulong expectedRevision);
}
