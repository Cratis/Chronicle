// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using Cratis.Chronicle.Projections.Graphs;

#pragma warning disable SA1402, SA1649, CA1819 // The declarations of one graph live together, and an array of primitives is what a graph property is.
namespace Cratis.Chronicle.Integration.GraphProjections.Fluent;

/// <summary>
/// A person, with nothing but its shape: the fluent builder says everything else.
/// </summary>
/// <param name="PersonId">The identifier of the person.</param>
/// <param name="Name">The name.</param>
/// <param name="Tags">The tags.</param>
/// <param name="Score">The points.</param>
/// <param name="Visits">The visits.</param>
/// <param name="Registered">When the person was registered.</param>
/// <param name="Kind">A constant.</param>
public record Person(Guid PersonId, string Name, string[] Tags, int Score, int Visits, DateTimeOffset Registered, string Kind);

/// <summary>
/// A group.
/// </summary>
/// <param name="GroupId">The identifier of the group.</param>
/// <param name="Name">The name.</param>
/// <param name="MemberCount">The members.</param>
public record Group(Guid GroupId, string Name, int MemberCount);

/// <summary>
/// A place.
/// </summary>
/// <param name="PlaceId">The identifier of the place.</param>
public record Place(Guid PlaceId);

/// <summary>
/// A membership.
/// </summary>
/// <param name="PersonId">The person.</param>
/// <param name="GroupId">The group.</param>
/// <param name="Since">When it started.</param>
public record MemberOf([Source<Person>] Guid PersonId, [Target<Group>] Guid GroupId, DateTimeOffset Since);

/// <summary>
/// A visit.
/// </summary>
/// <param name="PersonId">The person.</param>
/// <param name="PlaceId">The place.</param>
/// <param name="VisitId">The identity of the visit.</param>
public record Visited([Source<Person>] Guid PersonId, [Target<Place>] Guid PlaceId, Guid VisitId);

/// <summary>
/// A greeting.
/// </summary>
/// <param name="PersonId">The person who greeted.</param>
/// <param name="OtherId">The person who was greeted.</param>
/// <param name="Times">How many times.</param>
/// <param name="Warmth">The accumulated warmth.</param>
public record Greeted([Source<Person>] Guid PersonId, [Target<Person>] Guid OtherId, int Times, int Warmth);

/// <summary>
/// The same graph as the model-bound one, declared with the fluent builder.
/// </summary>
[Graph("OrgFluent")]
public class FluentOrganization : IGraphProjection
{
    /// <inheritdoc/>
    public void Define(IGraphProjectionBuilder builder) => builder
        .Node<Person>(n => n
            .IdentifiedBy(p => p.PersonId)
            .From<OrgPersonRegistered>(b => b
                .Set(p => p.Registered).ToEventContextProperty(c => c.Occurred)
                .Set(p => p.Kind).ToValue("person"))
            .From<OrgPersonRenamed>()
            .From<OrgPointsAwarded>(b => b.Add(p => p.Score).ToEventProperty(e => e.Points))
            .From<OrgPointsRevoked>(b => b.Subtract(p => p.Score).ToEventProperty(e => e.Points))
            .From<OrgPlaceVisited>(b => b.Count(p => p.Visits))
            .RemovedWith<OrgPersonRemoved>(b => b.Detach()))
        .Node<Group>(n => n
            .IdentifiedBy(g => g.GroupId)
            .From<OrgGroupCreated>()
            .From<OrgMemberAdded>(b => b.UsingKey(e => e.GroupId).Increment(g => g.MemberCount))
            .From<OrgMemberRemoved>(b => b.UsingKey(e => e.GroupId).Decrement(g => g.MemberCount))
            .RemovedWith<OrgGroupDissolved>())
        .Node<Place>(n => n
            .IdentifiedBy(p => p.PlaceId)
            .From<OrgPlaceVisited>(b => b.UsingKey(e => e.PlaceId)))
        .Edge<MemberOf, Person, Group>(e => e
            .Named("MEMBER_OF")
            .From<OrgMemberAdded>(b => b
                .UsingSourceKey(x => x.PersonId)
                .UsingTargetKey(x => x.GroupId)
                .Set(m => m.Since).ToEventContextProperty(c => c.Occurred))
            .RemovedWith<OrgMemberRemoved>(b => b
                .UsingSourceKey(x => x.PersonId)
                .UsingTargetKey(x => x.GroupId)))
        .Edge<Visited, Person, Place>(e => e
            .Named("VISITED")
            .IdentifiedBy(v => v.VisitId)
            .From<OrgPlaceVisited>(b => b
                .UsingSourceKey(x => x.PersonId)
                .UsingTargetKey(x => x.PlaceId)
                .UsingIdentity(x => x.VisitId)))
        .Edge<Greeted, Person, Person>(e => e
            .Named("GREETED")
            .From<OrgPersonGreeted>(b => b
                .UsingSourceKey(x => x.PersonId)
                .UsingTargetKey(x => x.OtherId)
                .Increment(g => g.Times)
                .Add(g => g.Warmth).ToEventProperty(x => x.Warmth))
            .From<OrgGreetingRetracted>(b => b
                .UsingSourceKey(x => x.PersonId)
                .UsingTargetKey(x => x.OtherId)
                .Decrement(g => g.Times)
                .Subtract(g => g.Warmth).ToEventProperty(x => x.Warmth)));
}
