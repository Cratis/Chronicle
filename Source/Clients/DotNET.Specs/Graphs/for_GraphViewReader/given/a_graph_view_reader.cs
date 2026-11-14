// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Graphs;
using Cratis.Serialization;

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.given;

public class a_graph_view_reader : Specification
{
    internal static readonly Type[] OrganizationSchema = [typeof(Person), typeof(Group), typeof(MemberOf), typeof(PartOf), typeof(HasRole)];

    internal static readonly Type[] OrganizationViews =
    [
        typeof(PersonWithGroups),
        typeof(GroupMembership),
        typeof(GroupSummary),
        typeof(GroupWithMembers),
        typeof(GroupWithParent),
        typeof(MemberSummary),
        typeof(PersonRoles),
        typeof(RoleAssignment)
    ];

    internal static readonly Type[] SchemaWithPlaces = [.. OrganizationSchema, typeof(Place), typeof(Visited)];

    internal INamingPolicy _namingPolicy;

    internal GraphModel ModelOf(IEnumerable<Type> types)
    {
        var reader = new ModelBoundGraphReader(_namingPolicy);
        var all = types.ToArray();
        return new(
            ModelBoundGraphReader.GraphNameOf(all[0]),
            [.. all.Where(_ => _.IsDefined(typeof(NodeAttribute), false)).Select(reader.ReadNode)],
            [.. all.Where(_ => _.IsDefined(typeof(EdgeAttribute), false)).Select(reader.ReadEdge)]);
    }

    internal GraphViews Read(IEnumerable<Type> schema, params Type[] views) => new GraphViewReader(_namingPolicy).Read(views, [ModelOf(schema)]);

    internal GraphViews Read(params Type[] views) => Read(SchemaWithPlaces, views);

    void Establish() => _namingPolicy = new CamelCaseNamingPolicy();
}
