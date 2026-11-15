// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Graphs;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.Graphs;
using Cratis.Chronicle.Projections.ModelBound;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
namespace Cratis.Chronicle.Testing.Graphs.for_GraphScenario;

[EventType]
public record OrganizationPersonRegistered(Guid PersonId, string Name);

[EventType]
public record OrganizationGroupCreated(Guid GroupId, string Name);

[EventType]
public record OrganizationMemberJoined(Guid PersonId, Guid GroupId);

[EventType]
public record OrganizationGroupNested(Guid ChildId, Guid ParentId);

[EventType]
public record OrganizationRoleAssigned(Guid PersonId, Guid GroupId, Guid AssignmentId, string Role);

public record PersonId(Guid Value) : ConceptAs<Guid>(Value);

public record PersonName(string Value) : ConceptAs<string>(Value);

public record GroupId(Guid Value) : ConceptAs<Guid>(Value);

public record GroupName(string Value) : ConceptAs<string>(Value);

[Graph("Organization")]
[Node]
[FromEvent<OrganizationPersonRegistered>]
public record Person([Key] Guid PersonId, string Name);

[Graph("Organization")]
[Node]
[FromEvent<OrganizationGroupCreated>]
public record Group([Key] Guid GroupId, string Name);

[Graph("Organization")]
[Edge("MEMBER_OF")]
[EdgeFrom<OrganizationMemberJoined>(sourceKey: nameof(OrganizationMemberJoined.PersonId), targetKey: nameof(OrganizationMemberJoined.GroupId))]
public record MemberOf(
    [Source<Person>] Guid PersonId,
    [Target<Group>] Guid GroupId,
    [SetFromContext<OrganizationMemberJoined>(nameof(EventContext.Occurred))] DateTimeOffset Since);

[Graph("Organization")]
[Edge("PART_OF")]
[EdgeFrom<OrganizationGroupNested>(sourceKey: nameof(OrganizationGroupNested.ChildId), targetKey: nameof(OrganizationGroupNested.ParentId))]
public record PartOf([Source<Group>] Guid ChildId, [Target<Group>] Guid ParentId);

[Graph("Organization")]
[Edge("HAS_ROLE")]
[EdgeFrom<OrganizationRoleAssigned>(
    sourceKey: nameof(OrganizationRoleAssigned.PersonId),
    targetKey: nameof(OrganizationRoleAssigned.GroupId),
    identity: nameof(OrganizationRoleAssigned.AssignmentId))]
public record HasRole(
    [Source<Person>] Guid PersonId,
    [Target<Group>] Guid GroupId,
    [Key] Guid AssignmentId,
    string Role,
    [SetFromContext<OrganizationRoleAssigned>(nameof(EventContext.Occurred))] DateTimeOffset Since);

[ViewOf<Person>]
public record PersonWithGroups([Key] PersonId Id, PersonName? Name, [Follows<MemberOf>] IReadOnlyList<GroupMembership> Groups);

[ViewOf<Group>]
public record GroupMembership(GroupId Id, GroupName? Name, [FromEdge] DateTimeOffset Since);

[ViewOf<Group>]
public record GroupSummary(GroupId Id, GroupName? Name);

[ViewOf<Group>]
public record GroupWithMembers(GroupId Id, GroupName? Name, [Follows<MemberOf>] IReadOnlyList<MemberSummary> Members);

[ViewOf<Group>]
public record GroupWithParent(GroupId Id, GroupName? Name, [Follows<PartOf>(GraphViewDirection.Outgoing)] GroupSummary? Parent);

[ViewOf<Person>]
public record MemberSummary(PersonId Id, PersonName? Name, [FromEdge] DateTimeOffset Since);

[ViewOf<Person>]
public record PersonRoles(PersonId Id, [Follows<HasRole>] IReadOnlyList<RoleAssignment> Roles);

[ViewOf<Group>]
public record RoleAssignment(GroupId Id, [FromEdge] Guid AssignmentId, [FromEdge] string Role);
