// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// The exception that is thrown when projection registration fails.
/// </summary>
/// <param name="eventStore">The event store where registration failed.</param>
/// <param name="projectionIds">The projection identifiers that were being registered.</param>
/// <param name="innerException">The underlying cause.</param>
public class ProjectionRegistrationFailed(EventStoreName eventStore, IEnumerable<ProjectionId> projectionIds, Exception innerException)
    : Exception(CreateMessage(eventStore, projectionIds, innerException), innerException)
{
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
