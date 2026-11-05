// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Sinks;
using Cratis.Serialization;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Graphs.for_GraphProjections;

public class when_discovering : Specification
{
    GraphProjections _graphs;

    void Establish()
    {
        var namingPolicy = new CamelCaseNamingPolicy();
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.GraphElements.Returns([
            typeof(for_GraphDefinitionBuilder.given.ModelBound.Person),
            typeof(for_GraphDefinitionBuilder.given.ModelBound.Group),
            typeof(for_GraphDefinitionBuilder.given.ModelBound.Place),
            typeof(for_GraphDefinitionBuilder.given.ModelBound.MemberOf),
            typeof(for_GraphDefinitionBuilder.given.ModelBound.Visited)
        ]);
        artifacts.GraphProjections.Returns([typeof(for_GraphDefinitionBuilder.given.Fluent.OrganizationGraph)]);
        var eventTypes = new EventTypesForSpecifications([
            typeof(for_GraphDefinitionBuilder.given.PersonRegistered),
            typeof(for_GraphDefinitionBuilder.given.PersonRemoved),
            typeof(for_GraphDefinitionBuilder.given.GroupCreated),
            typeof(for_GraphDefinitionBuilder.given.MemberAdded),
            typeof(for_GraphDefinitionBuilder.given.MemberRemoved),
            typeof(for_GraphDefinitionBuilder.given.PlaceVisited)
        ]);
        var generator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()),
            namingPolicy);
        _graphs = new GraphProjections(
            artifacts,
            new ClientArtifactsActivator(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance),
            namingPolicy,
            eventTypes,
            generator,
            WellKnownSinkTypes.InMemoryGraph);
    }

    void Because() => _graphs.Discover();

    [Fact] void should_register_one_graph_per_declaration_style() => _graphs.Registrations.Count.ShouldEqual(2);
    [Fact] void should_not_fail() => _graphs.Failures.ShouldBeEmpty();
    [Fact] void should_use_the_graph_sink() => _graphs.Registrations.All(_ => _.ReadModel.Sink.TypeId == "InMemoryGraph").ShouldBeTrue();
    [Fact] void should_identify_the_model_bound_projection_by_the_graph_name() => _graphs.Registrations.Any(_ => _.Projection.Identifier == "Organization").ShouldBeTrue();
    [Fact] void should_identify_the_fluent_projection_by_its_type() => _graphs.Registrations.Any(_ => _.Projection.Identifier == typeof(for_GraphDefinitionBuilder.given.Fluent.OrganizationGraph).FullName).ShouldBeTrue();
}
