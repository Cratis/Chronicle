// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.Graphs;
using Cratis.Chronicle.Projections.ModelBound;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
namespace Cratis.Chronicle.Testing.Graphs.for_GraphScenario;

[EventType]
public record ScenarioPersonRegistered(Guid PersonId, string Name);

[EventType]
public record ScenarioPersonRemoved(Guid PersonId);

[EventType]
public record ScenarioGroupCreated(Guid GroupId, string Name);

[EventType]
public record ScenarioMemberAdded(Guid PersonId, Guid GroupId);

[EventType]
public record ScenarioMemberRemoved(Guid PersonId, Guid GroupId);

[Graph("ScenarioOrganization")]
[Node]
[FromEvent<ScenarioPersonRegistered>]
[RemovedWith<ScenarioPersonRemoved>(detach: true)]
public record ScenarioPerson([Key] Guid PersonId, string Name);

[Graph("ScenarioOrganization")]
[Node]
[FromEvent<ScenarioGroupCreated>]
[FromEvent<ScenarioMemberAdded>(nameof(ScenarioMemberAdded.GroupId))]
public record ScenarioGroup([Key] Guid GroupId, string Name, [Increment<ScenarioMemberAdded>] int MemberCount);

[Graph("ScenarioOrganization")]
[Edge("SCENARIO_MEMBER_OF")]
[EdgeFrom<ScenarioMemberAdded>(sourceKey: nameof(ScenarioMemberAdded.PersonId), targetKey: nameof(ScenarioMemberAdded.GroupId))]
[EdgeRemovedWith<ScenarioMemberRemoved>(sourceKey: nameof(ScenarioMemberRemoved.PersonId), targetKey: nameof(ScenarioMemberRemoved.GroupId))]
public record ScenarioMemberOf(
    [Source<ScenarioPerson>] Guid PersonId,
    [Target<ScenarioGroup>] Guid GroupId,
    [SetFromContext<ScenarioMemberAdded>(nameof(EventContext.Occurred))] DateTimeOffset Since);
