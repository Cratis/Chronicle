// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// The exception that is thrown when projection registration fails.
/// </summary>
public class ProjectionRegistrationFailed : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionRegistrationFailed"/> class for a failure that names the
    /// definition it is attributed to.
    /// </summary>
    /// <param name="eventStore">The event store where registration failed.</param>
    /// <param name="projectionId">The projection identifier that failed to register.</param>
    /// <param name="innerException">The underlying cause.</param>
    public ProjectionRegistrationFailed(EventStoreName eventStore, ProjectionId projectionId, Exception innerException)
        : base(CreateMessage(eventStore, projectionId, innerException), innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionRegistrationFailed"/> class for a failure that cannot be
    /// attributed to a single definition.
    /// </summary>
    /// <param name="eventStore">The event store where registration failed.</param>
    /// <param name="projectionIds">The projection identifiers that were being registered.</param>
    /// <param name="innerException">The underlying cause.</param>
    public ProjectionRegistrationFailed(EventStoreName eventStore, IEnumerable<ProjectionId> projectionIds, Exception innerException)
        : base(CreateMessage(eventStore, projectionIds, innerException), innerException)
    {
    }

    /// <summary>
    /// Create a <see cref="ProjectionRegistrationFailed"/> that names as little of the batch as the failure allows.
    /// </summary>
    /// <param name="eventStore">The event store where registration failed.</param>
    /// <param name="projectionIds">The projection identifiers that were being registered.</param>
    /// <param name="innerException">The underlying cause.</param>
    /// <returns>A <see cref="ProjectionRegistrationFailed"/> for the failure.</returns>
    /// <remarks>
    /// A failure raised while registering one definition carries that definition's identifier out with it. Anything
    /// raised before the batch is broken down - reading the event type schemas, for instance - belongs to no single
    /// definition, and only then does naming the whole batch say something true.
    /// </remarks>
    public static ProjectionRegistrationFailed For(EventStoreName eventStore, IEnumerable<ProjectionId> projectionIds, Exception innerException) =>
        ProjectionDefinitionRegistrationFailed.TryFindIdentifier(innerException, out var identifier)
            ? new ProjectionRegistrationFailed(eventStore, identifier, innerException)
            : new ProjectionRegistrationFailed(eventStore, projectionIds, innerException);

    static string CreateMessage(EventStoreName eventStore, ProjectionId projectionId, Exception exception) =>
        $"Failed to register projection '{projectionId}' for event store '{eventStore}'. Root cause: {exception.GetBaseException().Message}";

    static string CreateMessage(EventStoreName eventStore, IEnumerable<ProjectionId> projectionIds, Exception exception)
    {
        var identifiers = string.Join(", ", projectionIds.Select(_ => _.Value));
        if (string.IsNullOrWhiteSpace(identifiers))
        {
            identifiers = "(none)";
        }

        return $"Failed to register projection(s) [{identifiers}] for event store '{eventStore}'. Root cause: {exception.GetBaseException().Message}";
    }
}
