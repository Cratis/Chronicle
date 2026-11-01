// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Represents a durable observer registration at event-store scope.
/// </summary>
/// <param name="ObserverId">The observer identifier.</param>
/// <param name="EventSequenceId">The event sequence identifier.</param>
/// <param name="ObserverType">The observer type.</param>
/// <param name="Definition">The complete definition used to restore the observer.</param>
public record ObserverRegistration(
    ObserverId ObserverId,
    EventSequenceId EventSequenceId,
    ObserverType ObserverType,
    ObserverRegistrationDefinition Definition)
{
    /// <summary>
    /// Gets the owning program.
    /// </summary>
    public ProgramIdentifier Owner { get; init; } = ProgramIdentifier.Unknown;

    /// <summary>
    /// Gets the authenticated owner principal.
    /// </summary>
    public OwnerPrincipal OwnerPrincipal { get; init; } = OwnerPrincipal.Anonymous;

    /// <summary>
    /// Gets the delivery target kind; omitted metadata retains stream delivery.
    /// </summary>
    public ObserverTargetKind TargetKind { get; init; } = ObserverTargetKind.Stream;

    /// <summary>
    /// Gets the HTTP endpoint, if any.
    /// </summary>
    public HttpTarget? HttpTarget { get; init; }

    /// <summary>
    /// Gets the registration source.
    /// </summary>
    public ObserverRegistrationSource Source { get; init; } = ObserverRegistrationSource.Stream;

    /// <summary>
    /// Gets whether this registration is active.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets the per-observer compare-and-set revision.
    /// </summary>
    public ObserverRegistrationRevision Revision { get; init; } = new(0);

    /// <summary>
    /// Gets the deployment generation that last wrote the registration.
    /// </summary>
    public ulong DeploymentGeneration { get; init; }

    /// <summary>
    /// Gets when the registration last changed.
    /// </summary>
    public DateTimeOffset LastChanged { get; init; }
}
