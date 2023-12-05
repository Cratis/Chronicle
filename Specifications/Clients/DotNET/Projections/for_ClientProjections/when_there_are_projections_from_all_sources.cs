// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Projections.for_ClientProjections;

public class when_there_are_projections_from_all_sources : given.all_dependencies
{
    ClientProjections definitions;
    IEnumerable<ProjectionDefinition> all;
    IEnumerable<ProjectionDefinition> projections_from_projections;
    IEnumerable<ProjectionDefinition> projections_from_immediate_projections;
    IEnumerable<ProjectionDefinition> projections_from_adapters;
    IEnumerable<ProjectionDefinition> projections_from_rules_projections;

    void Establish()
    {
        projections_from_projections = CreateProjectionDefinitions();
        projections_from_immediate_projections = CreateProjectionDefinitions();
        projections_from_adapters = CreateProjectionDefinitions();
        projections_from_rules_projections = CreateProjectionDefinitions();

        projections.Setup(_ => _.Definitions).Returns(projections_from_projections.ToImmutableList());
        immediate_projections.Setup(_ => _.Definitions).Returns(projections_from_immediate_projections.ToImmutableList());
        adapters.Setup(_ => _.Definitions).Returns(projections_from_adapters.ToImmutableList());
        rules_projections.Setup(_ => _.Definitions).Returns(projections_from_rules_projections.ToImmutableList());

        definitions = new(
            projections.Object,
            immediate_projections.Object,
            adapters.Object,
            rules_projections.Object);
    }

    void Because() => all = definitions.Definitions.ToArray();

    [Fact] void should_contain_projections_from_projections() => all.ShouldContain(projections_from_projections);
    [Fact] void should_contain_projections_from_immediate_projections() => all.ShouldContain(projections_from_immediate_projections);
    [Fact] void should_contain_projections_from_adapters() => all.ShouldContain(projections_from_adapters);
    [Fact] void should_contain_projections_from_rules_projections() => all.ShouldContain(projections_from_rules_projections);

    IEnumerable<ProjectionDefinition> CreateProjectionDefinitions() =>
        Enumerable.Range(0, 2).Select(_ => new
            ProjectionDefinition(
                Guid.NewGuid(),
                $"Projection {_}",
                new ModelDefinition(string.Empty, string.Empty),
                false,
                false,
                new JsonObject(),
                new Dictionary<EventType, FromDefinition>(),
                new Dictionary<EventType, JoinDefinition>(),
                new Dictionary<PropertyPath, ChildrenDefinition>(),
                Enumerable.Empty<FromAnyDefinition>(),
                new AllDefinition(new Dictionary<PropertyPath, string>(), false))).ToArray();
}
