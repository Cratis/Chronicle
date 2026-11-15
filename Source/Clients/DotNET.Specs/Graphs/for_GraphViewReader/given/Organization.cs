// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.Graphs;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
namespace Cratis.Chronicle.Graphs.for_GraphViewReader.given;

[Graph("Organization")]
[Node]
public record Person([Key] Guid PersonId, string Name);

[Graph("Organization")]
[Node]
public record Group([Key] Guid GroupId, string Name);

[Graph("Organization")]
[Node]
public record Place([Key] Guid PlaceId);

[Graph("Organization")]
[Edge("MEMBER_OF")]
public record MemberOf([Source<Person>] Guid PersonId, [Target<Group>] Guid GroupId, DateTimeOffset Since);

[Graph("Organization")]
[Edge("PART_OF")]
public record PartOf([Source<Group>] Guid ChildId, [Target<Group>] Guid ParentId);

[Graph("Organization")]
[Edge("HAS_ROLE")]
public record HasRole([Source<Person>] Guid PersonId, [Target<Group>] Guid GroupId, [Key] Guid AssignmentId, string Role, DateTimeOffset Since);

[Graph("Organization")]
[Edge("VISITED")]
public record Visited([Source<Place>] Guid FromId, [Target<Place>] Guid ToId);

[ViewOf<Person>]
public record PersonWithGroups([Key] Guid Id, string Name, [Follows<MemberOf>] IReadOnlyList<GroupMembership> Groups);

[ViewOf<Group>]
public record GroupMembership(Guid Id, string Name, [FromEdge] DateTimeOffset Since);

[ViewOf<Group>]
public record GroupSummary(Guid Id, string Name);

[ViewOf<Group>]
public record GroupWithMembers(Guid Id, string Name, [Follows<MemberOf>] IReadOnlyList<MemberSummary> Members);

[ViewOf<Group>]
public record GroupWithParent(Guid Id, string Name, [Follows<PartOf>(GraphViewDirection.Outgoing)] GroupSummary? Parent);

[ViewOf<Person>]
public record MemberSummary(Guid Id, string Name, [FromEdge] DateTimeOffset Since);

[ViewOf<Person>]
public record PersonRoles(Guid Id, [Follows<HasRole>] IReadOnlyList<RoleAssignment> Roles);

[ViewOf<Group>]
public record RoleAssignment(Guid Id, [FromEdge] Guid AssignmentId, [FromEdge] string Role);
