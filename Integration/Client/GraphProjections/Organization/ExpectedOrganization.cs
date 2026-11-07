// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.GraphProjections.Organization;

/// <summary>
/// The graph every declaration of the organization must leave behind after the story is told.
/// </summary>
/// <remarks>
/// Ada earned 10 and 5 points and lost 3, visited the lobby twice, greeted Bob twice and took one greeting back, and was renamed.
/// Bob joined engineering and left it again, and joined operations. Cy was greeted by Ada, joined engineering and was removed, and
/// took the edges with him. The member count of engineering stays at two because nothing took the removed person out of it.
/// </remarks>
public static class ExpectedOrganization
{
    /// <summary>
    /// Gets the description of the graph, as <see cref="GraphSnapshot.Describe"/> writes it.
    /// </summary>
    public static readonly string[] Graph =
    [
        "edge GREETED (Person {kind: person, name: Ada L., personId: aa000000-0000-4000-8000-000000000001, registered: 2026-01-01T08:00:00.0000000Z, score: 12, tags: [admin, dev], visits: 2}) -> (Person {kind: person, name: Bob, personId: bb000000-0000-4000-8000-000000000002, registered: 2026-01-02T08:00:00.0000000Z, tags: [dev], visits: 1}) {times: 1, warmth: 4}",
        "edge MEMBER_OF (Person {kind: person, name: Ada L., personId: aa000000-0000-4000-8000-000000000001, registered: 2026-01-01T08:00:00.0000000Z, score: 12, tags: [admin, dev], visits: 2}) -> (Group {groupId: e0000000-0000-4000-8000-000000000011, memberCount: 2, name: Engineering}) {since: 2026-01-06T08:00:00.0000000Z}",
        "edge MEMBER_OF (Person {kind: person, name: Bob, personId: bb000000-0000-4000-8000-000000000002, registered: 2026-01-02T08:00:00.0000000Z, tags: [dev], visits: 1}) -> (Group {groupId: e0000000-0000-4000-8000-000000000012, memberCount: 1, name: Operations}) {since: 2026-01-08T08:00:00.0000000Z}",
        "edge VISITED (Person {kind: person, name: Ada L., personId: aa000000-0000-4000-8000-000000000001, registered: 2026-01-01T08:00:00.0000000Z, score: 12, tags: [admin, dev], visits: 2}) -> (Place {placeId: f0000000-0000-4000-8000-000000000021}) {visitId: d0000000-0000-4000-8000-000000000031}",
        "edge VISITED (Person {kind: person, name: Ada L., personId: aa000000-0000-4000-8000-000000000001, registered: 2026-01-01T08:00:00.0000000Z, score: 12, tags: [admin, dev], visits: 2}) -> (Place {placeId: f0000000-0000-4000-8000-000000000021}) {visitId: d0000000-0000-4000-8000-000000000032}",
        "edge VISITED (Person {kind: person, name: Bob, personId: bb000000-0000-4000-8000-000000000002, registered: 2026-01-02T08:00:00.0000000Z, tags: [dev], visits: 1}) -> (Place {placeId: f0000000-0000-4000-8000-000000000022}) {visitId: d0000000-0000-4000-8000-000000000033}",
        "node Group {groupId: e0000000-0000-4000-8000-000000000011, memberCount: 2, name: Engineering}",
        "node Group {groupId: e0000000-0000-4000-8000-000000000012, memberCount: 1, name: Operations}",
        "node Person {kind: person, name: Ada L., personId: aa000000-0000-4000-8000-000000000001, registered: 2026-01-01T08:00:00.0000000Z, score: 12, tags: [admin, dev], visits: 2}",
        "node Person {kind: person, name: Bob, personId: bb000000-0000-4000-8000-000000000002, registered: 2026-01-02T08:00:00.0000000Z, tags: [dev], visits: 1}",
        "node Place {placeId: f0000000-0000-4000-8000-000000000021}",
        "node Place {placeId: f0000000-0000-4000-8000-000000000022}"
    ];
}
