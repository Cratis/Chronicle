// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
namespace Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.given.Fluent;

public record Person(Guid PersonId, string Name);

public record Group(Guid GroupId, string Name, int MemberCount);

public record Place(Guid PlaceId);

[Edge("MEMBER_OF")]
public record MemberOf([Source<Person>] Guid PersonId, [Target<Group>] Guid GroupId, DateTimeOffset Since);

[Edge("VISITED")]
public record Visited([Source<Person>] Guid PersonId, [Target<Place>] Guid PlaceId, [Key] Guid VisitId, int Times);

[Graph("Organization")]
public class OrganizationGraph : IGraphProjection
{
    public void Define(IGraphProjectionBuilder builder) => builder
        .Node<Person>(n => n
            .IdentifiedBy(p => p.PersonId)
            .From<PersonRegistered>()
            .RemovedWith<PersonRemoved>(b => b.Detach()))
        .Node<Group>(n => n
            .IdentifiedBy(g => g.GroupId)
            .From<GroupCreated>()
            .From<MemberAdded>(b => b.UsingKey(e => e.GroupId).Increment(g => g.MemberCount))
            .From<MemberRemoved>(b => b.UsingKey(e => e.GroupId).Decrement(g => g.MemberCount)))
        .Node<Place>(n => n
            .IdentifiedBy(p => p.PlaceId)
            .From<PlaceVisited>(b => b.UsingKey(e => e.PlaceId)))
        .Edge<MemberOf, Person, Group>(e => e
            .From<MemberAdded>(b => b
                .UsingSourceKey(x => x.PersonId)
                .UsingTargetKey(x => x.GroupId)
                .Set(m => m.Since).ToEventContextProperty(c => c.Occurred))
            .RemovedWith<MemberRemoved>(b => b
                .UsingSourceKey(x => x.PersonId)
                .UsingTargetKey(x => x.GroupId)))
        .Edge<Visited, Person, Place>(e => e
            .From<PlaceVisited>(b => b
                .UsingSourceKey(x => x.PersonId)
                .UsingTargetKey(x => x.PlaceId)
                .UsingIdentity(x => x.VisitId)
                .Count(v => v.Times)));
}
