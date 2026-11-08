// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Sinks;
using Cratis.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Graphs.for_GraphProjections.given;

internal static class graph_projections
{
    internal static GraphProjections For(IEnumerable<Type> elements, IEnumerable<Type> projections, string defaultSinkName)
    {
        var namingPolicy = new CamelCaseNamingPolicy();
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.GraphElements.Returns(elements);
        artifacts.GraphProjections.Returns(projections);
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
        return new GraphProjections(
            artifacts,
            new ClientArtifactsActivator(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance),
            namingPolicy,
            eventTypes,
            generator,
            WellKnownSinkTypes.Neo4j,
            defaultSinkName);
    }
}
