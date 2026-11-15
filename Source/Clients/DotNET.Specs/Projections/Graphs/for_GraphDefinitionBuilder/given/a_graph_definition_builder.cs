// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.given;

public class a_graph_definition_builder : Specification
{
    internal INamingPolicy _namingPolicy;
    internal IEventTypes _eventTypes;
    internal GraphDefinitionBuilder _builder;

    internal static JsonNode Canonical(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(obj.OrderBy(_ => _.Key, StringComparer.Ordinal).Select(_ => KeyValuePair.Create(_.Key, _.Value is null ? null : Canonical(_.Value)))),
        JsonArray array => new JsonArray(array.Select(_ => _ is null ? null : Canonical(_)).ToArray()),
        _ => node!.DeepClone()
    };

    internal static string CanonicalJson(object value) => Canonical(JsonSerializer.SerializeToNode(value)).ToJsonString();

    internal GraphModel ModelBoundModel() => ModelBoundModelOf(
        typeof(given.ModelBound.Person),
        typeof(given.ModelBound.Group),
        typeof(given.ModelBound.Place),
        typeof(given.ModelBound.MemberOf),
        typeof(given.ModelBound.Visited));

    internal GraphModel ModelBoundModelOf(params Type[] types)
    {
        var reader = new ModelBoundGraphReader(_namingPolicy);
        return new(
            ModelBoundGraphReader.GraphNameOf(types[0]),
            [.. types.Where(_ => _.IsDefined(typeof(NodeAttribute), false)).Select(reader.ReadNode)],
            [.. types.Where(_ => _.IsDefined(typeof(EdgeAttribute), false)).Select(reader.ReadEdge)]);
    }

    internal GraphModel FluentModel() => new GraphProjectionBuilder(_namingPolicy).Build(new given.Fluent.OrganizationGraph());

    void Establish()
    {
        _namingPolicy = new CamelCaseNamingPolicy();
        _eventTypes = new EventTypesForSpecifications([
            typeof(given.PersonRegistered),
            typeof(given.PersonRemoved),
            typeof(given.GroupCreated),
            typeof(given.MemberAdded),
            typeof(given.MemberRemoved),
            typeof(given.PlaceVisited)
        ]);
        var generator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()),
            _namingPolicy);
        _builder = new GraphDefinitionBuilder(_namingPolicy, _eventTypes, generator);
    }
}
