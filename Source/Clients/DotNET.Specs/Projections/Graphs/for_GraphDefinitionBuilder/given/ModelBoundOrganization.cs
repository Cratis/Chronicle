// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
namespace Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.given.ModelBound;

[Graph("Organization")]
[Node]
[FromEvent<PersonRegistered>]
[RemovedWith<PersonRemoved>(detach: true)]
public record Person([Key] Guid PersonId, string Name);

[Graph("Organization")]
[Node]
[FromEvent<GroupCreated>]
[FromEvent<MemberAdded>(nameof(MemberAdded.GroupId))]
[FromEvent<MemberRemoved>(nameof(MemberRemoved.GroupId))]
public record Group(
    [Key] Guid GroupId,
    string Name,
    [Increment<MemberAdded>] [Decrement<MemberRemoved>] int MemberCount);

[Graph("Organization")]
[Node]
[FromEvent<PlaceVisited>(nameof(PlaceVisited.PlaceId))]
public record Place([Key] Guid PlaceId);

[Graph("Organization")]
[Edge("MEMBER_OF")]
[EdgeFrom<MemberAdded>(sourceKey: nameof(MemberAdded.PersonId), targetKey: nameof(MemberAdded.GroupId))]
[EdgeRemovedWith<MemberRemoved>(sourceKey: nameof(MemberRemoved.PersonId), targetKey: nameof(MemberRemoved.GroupId))]
public record MemberOf(
    [Source<Person>] Guid PersonId,
    [Target<Group>] Guid GroupId,
    [SetFromContext<MemberAdded>(nameof(EventContext.Occurred))] DateTimeOffset Since);

[Graph("Organization")]
[Edge("VISITED")]
[EdgeFrom<PlaceVisited>(sourceKey: nameof(PlaceVisited.PersonId), targetKey: nameof(PlaceVisited.PlaceId), identity: nameof(PlaceVisited.VisitId))]
public record Visited(
    [Source<Person>] Guid PersonId,
    [Target<Place>] Guid PlaceId,
    [Key] Guid VisitId,
    [Count<PlaceVisited>] int Times);
