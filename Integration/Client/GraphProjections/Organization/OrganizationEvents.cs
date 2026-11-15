// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

#pragma warning disable SA1402, SA1649, CA1819 // The declarations of one graph live together, and an array of primitives is what a graph property is.
namespace Cratis.Chronicle.Integration.GraphProjections.Organization;

/// <summary>
/// A person was registered.
/// </summary>
/// <param name="PersonId">The identifier of the person.</param>
/// <param name="Name">The name of the person.</param>
/// <param name="Tags">The tags of the person.</param>
[EventType]
public record OrgPersonRegistered(Guid PersonId, string Name, string[] Tags);

/// <summary>
/// A person was renamed.
/// </summary>
/// <param name="PersonId">The identifier of the person.</param>
/// <param name="Name">The new name.</param>
[EventType]
public record OrgPersonRenamed(Guid PersonId, string Name);

/// <summary>
/// A person was removed.
/// </summary>
/// <param name="PersonId">The identifier of the person.</param>
[EventType]
public record OrgPersonRemoved(Guid PersonId);

/// <summary>
/// A person earned points.
/// </summary>
/// <param name="PersonId">The identifier of the person.</param>
/// <param name="Points">The points.</param>
[EventType]
public record OrgPointsAwarded(Guid PersonId, int Points);

/// <summary>
/// A person lost points.
/// </summary>
/// <param name="PersonId">The identifier of the person.</param>
/// <param name="Points">The points.</param>
[EventType]
public record OrgPointsRevoked(Guid PersonId, int Points);

/// <summary>
/// A group was created.
/// </summary>
/// <param name="GroupId">The identifier of the group.</param>
/// <param name="Name">The name of the group.</param>
[EventType]
public record OrgGroupCreated(Guid GroupId, string Name);

/// <summary>
/// A group was dissolved.
/// </summary>
/// <param name="GroupId">The identifier of the group.</param>
[EventType]
public record OrgGroupDissolved(Guid GroupId);

/// <summary>
/// A person became a member of a group.
/// </summary>
/// <param name="PersonId">The identifier of the person.</param>
/// <param name="GroupId">The identifier of the group.</param>
[EventType]
public record OrgMemberAdded(Guid PersonId, Guid GroupId);

/// <summary>
/// A person stopped being a member of a group.
/// </summary>
/// <param name="PersonId">The identifier of the person.</param>
/// <param name="GroupId">The identifier of the group.</param>
[EventType]
public record OrgMemberRemoved(Guid PersonId, Guid GroupId);

/// <summary>
/// A person visited a place.
/// </summary>
/// <param name="PersonId">The identifier of the person.</param>
/// <param name="PlaceId">The identifier of the place.</param>
/// <param name="VisitId">The identifier of the visit.</param>
[EventType]
public record OrgPlaceVisited(Guid PersonId, Guid PlaceId, Guid VisitId);

/// <summary>
/// A person greeted another person.
/// </summary>
/// <param name="PersonId">The identifier of the person who greeted.</param>
/// <param name="OtherId">The identifier of the person who was greeted.</param>
/// <param name="Warmth">How warm the greeting was.</param>
[EventType]
public record OrgPersonGreeted(Guid PersonId, Guid OtherId, int Warmth);

/// <summary>
/// A greeting was taken back.
/// </summary>
/// <param name="PersonId">The identifier of the person who greeted.</param>
/// <param name="OtherId">The identifier of the person who was greeted.</param>
/// <param name="Warmth">How warm the greeting that is taken back was.</param>
[EventType]
public record OrgGreetingRetracted(Guid PersonId, Guid OtherId, int Warmth);

/// <summary>
/// The events every graph projection declaration of the organization listens to.
/// </summary>
public static class OrganizationEventTypes
{
    /// <summary>
    /// Gets all the event types.
    /// </summary>
    public static readonly Type[] All =
    [
        typeof(OrgPersonRegistered),
        typeof(OrgPersonRenamed),
        typeof(OrgPersonRemoved),
        typeof(OrgPointsAwarded),
        typeof(OrgPointsRevoked),
        typeof(OrgGroupCreated),
        typeof(OrgGroupDissolved),
        typeof(OrgMemberAdded),
        typeof(OrgMemberRemoved),
        typeof(OrgPlaceVisited),
        typeof(OrgPersonGreeted),
        typeof(OrgGreetingRetracted)
    ];
}
