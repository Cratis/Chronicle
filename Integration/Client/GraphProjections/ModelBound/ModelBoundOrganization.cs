// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.GraphProjections.Organization;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.Graphs;
using Cratis.Chronicle.Projections.ModelBound;

#pragma warning disable SA1402, SA1649, CA1819 // The declarations of one graph live together, and an array of primitives is what a graph property is.
namespace Cratis.Chronicle.Integration.GraphProjections.ModelBound;

/// <summary>
/// A person, declared with the model-bound attributes.
/// </summary>
/// <param name="PersonId">The identifier of the person.</param>
/// <param name="Name">The name.</param>
/// <param name="Tags">The tags, an array of primitives.</param>
/// <param name="Score">The points the person has, added and subtracted from events.</param>
/// <param name="Visits">How many places the person visited.</param>
/// <param name="Registered">When the person was registered, from the event context.</param>
/// <param name="Kind">A constant.</param>
[Graph("OrgModelBound")]
[Node]
[FromEvent<OrgPersonRegistered>]
[FromEvent<OrgPersonRenamed>]
[FromEvent<OrgPointsAwarded>]
[FromEvent<OrgPointsRevoked>]
[FromEvent<OrgPlaceVisited>]
[RemovedWith<OrgPersonRemoved>(detach: true)]
public record Person(
    [Key] Guid PersonId,
    string Name,
    string[] Tags,
    [AddFrom<OrgPointsAwarded>(nameof(OrgPointsAwarded.Points))] [SubtractFrom<OrgPointsRevoked>(nameof(OrgPointsRevoked.Points))] int Score,
    [Count<OrgPlaceVisited>] int Visits,
    [SetFromContext<OrgPersonRegistered>(nameof(EventContext.Occurred))] DateTimeOffset Registered,
    [SetValue<OrgPersonRegistered>("person")] string Kind);

/// <summary>
/// A group.
/// </summary>
/// <param name="GroupId">The identifier of the group.</param>
/// <param name="Name">The name.</param>
/// <param name="MemberCount">How many members the group has.</param>
[Graph("OrgModelBound")]
[Node]
[FromEvent<OrgGroupCreated>]
[FromEvent<OrgMemberAdded>(nameof(OrgMemberAdded.GroupId))]
[FromEvent<OrgMemberRemoved>(nameof(OrgMemberRemoved.GroupId))]
[RemovedWith<OrgGroupDissolved>]
public record Group(
    [Key] Guid GroupId,
    string Name,
    [Increment<OrgMemberAdded>] [Decrement<OrgMemberRemoved>] int MemberCount);

/// <summary>
/// A place.
/// </summary>
/// <param name="PlaceId">The identifier of the place.</param>
[Graph("OrgModelBound")]
[Node]
[FromEvent<OrgPlaceVisited>(nameof(OrgPlaceVisited.PlaceId))]
public record Place([Key] Guid PlaceId);

/// <summary>
/// The membership of a person in a group.
/// </summary>
/// <param name="PersonId">The person.</param>
/// <param name="GroupId">The group.</param>
/// <param name="Since">When the membership started, from the event context.</param>
[Graph("OrgModelBound")]
[Edge("MEMBER_OF")]
[EdgeFrom<OrgMemberAdded>(sourceKey: nameof(OrgMemberAdded.PersonId), targetKey: nameof(OrgMemberAdded.GroupId))]
[EdgeRemovedWith<OrgMemberRemoved>(sourceKey: nameof(OrgMemberRemoved.PersonId), targetKey: nameof(OrgMemberRemoved.GroupId))]
public record MemberOf(
    [Source<Person>] Guid PersonId,
    [Target<Group>] Guid GroupId,
    [SetFromContext<OrgMemberAdded>(nameof(EventContext.Occurred))] DateTimeOffset Since);

/// <summary>
/// A visit of a person to a place. A person can visit the same place many times, so each visit has an identity.
/// </summary>
/// <param name="PersonId">The person.</param>
/// <param name="PlaceId">The place.</param>
/// <param name="VisitId">The identity of the visit.</param>
[Graph("OrgModelBound")]
[Edge("VISITED")]
[EdgeFrom<OrgPlaceVisited>(sourceKey: nameof(OrgPlaceVisited.PersonId), targetKey: nameof(OrgPlaceVisited.PlaceId), identity: nameof(OrgPlaceVisited.VisitId))]
public record Visited(
    [Source<Person>] Guid PersonId,
    [Target<Place>] Guid PlaceId,
    [Key] Guid VisitId);

/// <summary>
/// A person greeting another person, which counts and accumulates on the edge.
/// </summary>
/// <param name="PersonId">The person who greeted.</param>
/// <param name="OtherId">The person who was greeted.</param>
/// <param name="Times">How many times the person greeted the other.</param>
/// <param name="Warmth">The accumulated warmth.</param>
[Graph("OrgModelBound")]
[Edge("GREETED")]
[EdgeFrom<OrgPersonGreeted>(sourceKey: nameof(OrgPersonGreeted.PersonId), targetKey: nameof(OrgPersonGreeted.OtherId))]
[EdgeFrom<OrgGreetingRetracted>(sourceKey: nameof(OrgGreetingRetracted.PersonId), targetKey: nameof(OrgGreetingRetracted.OtherId))]
public record Greeted(
    [Source<Person>] Guid PersonId,
    [Target<Person>] Guid OtherId,
    [Increment<OrgPersonGreeted>] [Decrement<OrgGreetingRetracted>] int Times,
    [AddFrom<OrgPersonGreeted>(nameof(OrgPersonGreeted.Warmth))] [SubtractFrom<OrgGreetingRetracted>(nameof(OrgGreetingRetracted.Warmth))] int Warmth);

/// <summary>
/// The types of the organization declared with the model-bound attributes.
/// </summary>
public static class ModelBoundOrganizationTypes
{
    /// <summary>
    /// Gets the nodes and edges.
    /// </summary>
    public static readonly Type[] All = [typeof(Person), typeof(Group), typeof(Place), typeof(MemberOf), typeof(Visited), typeof(Greeted)];
}
