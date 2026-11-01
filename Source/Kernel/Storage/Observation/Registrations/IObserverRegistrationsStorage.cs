// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Registrations;

namespace Cratis.Chronicle.Storage.Observation.Registrations;

/// <summary>
/// Stores registrations, owner settings and the deployment generation floor for one event store.
/// Reads never create records; missing registrations retain legacy behavior.
/// Deleted registrations retain a revision tombstone that is absent from every read and query.
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
    /// Recreating a deleted registration requires the tombstone revision: the last live revision plus one.
    /// The recreated revision must advance that tombstone by one; zero never replaces a tombstone.
    /// </summary>
    /// <param name="registration">The complete registration.</param>
    /// <param name="expectedRevision">The stored live or tombstone revision; zero requires a never-created record.</param>
    /// <returns>Whether the write succeeded.</returns>
    Task<bool> TrySave(ObserverRegistration registration, ObserverRegistrationRevision expectedRevision);

    /// <summary>
    /// Deletes a live registration only if its revision matches, retaining a tombstone at revision plus one.
    /// The successful caller can use that tombstone revision to recreate the registration.
    /// Missing or already deleted registrations and revisions that cannot advance are rejected.
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
