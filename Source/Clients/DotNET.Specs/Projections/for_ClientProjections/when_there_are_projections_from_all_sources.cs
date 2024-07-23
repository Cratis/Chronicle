// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Models;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Models;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ClientProjections;

public class when_there_are_projections_from_all_sources : given.all_dependencies
{
    ClientProjections definitions;
    IEnumerable<ProjectionDefinition> all;
    IEnumerable<ProjectionDefinition> projections_from_projections;
    IEnumerable<ProjectionDefinition> projections_from_adapters;
    IEnumerable<ProjectionDefinition> projections_from_rules_projections;

    void Establish()
    {
        projections_from_projections = CreateProjectionDefinitions();
        projections_from_adapters = CreateProjectionDefinitions();
        projections_from_rules_projections = CreateProjectionDefinitions();

        projections.Setup(_ => _.Definitions).Returns(projections_from_projections.ToImmutableList());
        adapters.Setup(_ => _.Definitions).Returns(projections_from_adapters.ToImmutableList());
        rules_projections.Setup(_ => _.Definitions).Returns(projections_from_rules_projections.ToImmutableList());

        definitions = new(
            projections.Object,
            adapters.Object,
            rules_projections.Object);
    }

    void Because() => all = definitions.Definitions;

    [Fact] void should_contain_projections_from_projections() => all.ShouldContain(projections_from_projections);
    [Fact] void should_contain_projections_from_adapters() => all.ShouldContain(projections_from_adapters);
    [Fact] void should_contain_projections_from_rules_projections() => all.ShouldContain(projections_from_rules_projections);

    ProjectionDefinition[] CreateProjectionDefinitions() =>
        Enumerable.Range(0, 2).Select(_ => new
            ProjectionDefinition
        {
            Identifier = Guid.NewGuid().ToString(),
            Model = new ModelDefinition
            {
                Name = string.Empty,
                Schema = string.Empty
            },
            IsActive = false,
            IsRewindable = false,
            InitialModelState = string.Empty,
            From = new Dictionary<Contracts.Events.EventType, FromDefinition>(),
            Join = new Dictionary<Contracts.Events.EventType, JoinDefinition>(),
            Children = new Dictionary<string, ChildrenDefinition>(),
            All = new AllDefinition
            {
                Properties = new Dictionary<string, string>(),
                IncludeChildren = false
            }
        }).ToArray();
}
