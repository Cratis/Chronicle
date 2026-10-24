// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Json;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationExecutionSpecifications;

public class when_capturing_execution : Specification
{
    GenerationExecutionSpecifications _factory;
    ProjectionDefinition _projection;
    ReadModelDefinition _model;
    Dictionary<PropertyPath, string> _properties;
    MaterializationExecutionSpecification _snapshot;
    void Establish()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ConceptAsJsonConverterFactory());
        _factory = new(options);
        _properties = new() { ["name"] = "name", ["count"] = "count" };
        _projection = new(ProjectionOwner.Client, EventSequenceId.Log, "projection", "model", true, true, new JsonObject(), new Dictionary<EventType, FromDefinition>(), new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [], new(_properties, true), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>()) { Version = "1.2.3" };
        _model = new("model", "models", "model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [new(1)] = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}") }, []);
    }
    void Because() => _snapshot = _factory.Capture(_projection, _model, [], new("sink-v1"));
    [Fact] void should_preserve_registering_version() => _snapshot.RegisteringVersion.ShouldEqual("1.2.3");
    [Fact] void should_include_full_schema_content() => _snapshot.CanonicalContent.Contains("\"properties\":{\"name\":{\"type\":\"string\"}}").ShouldBeTrue();
    [Fact] void should_exclude_operational_sink_configuration() => _snapshot.CanonicalContent.Contains("connectionString", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    [Fact] void should_be_detached_from_mutable_registration()
    {
        var original = _snapshot.CanonicalContent;
        _properties["name"] = "changed";
        _snapshot.CanonicalContent.ShouldEqual(original);
        _factory.Capture(_projection, _model, [], new("sink-v1")).Revision.ShouldNotEqual(_snapshot.Revision);
    }
    [Fact] void should_hash_semantic_sink_changes() => _factory.Capture(_projection, _model, [], new("sink-v2")).Revision.ShouldNotEqual(_snapshot.Revision);
    [Fact] void should_ignore_dictionary_insertion_order()
    {
        var reordered = _projection with { FromEvery = new(new Dictionary<PropertyPath, string> { ["count"] = "count", ["name"] = "name" }, true) };
        _factory.Capture(reordered, _model, [], new("sink-v1")).ShouldEqual(_snapshot);
    }
    [Fact] void should_capture_recursive_child_initial_state()
    {
        var child = new ChildrenDefinition("id", new Dictionary<EventType, FromDefinition>(), new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), new(new Dictionary<PropertyPath, string>(), true), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>()) { InitialModelState = new JsonObject { ["name"] = "child-initial" } };
        var projection = _projection with { Children = new Dictionary<PropertyPath, ChildrenDefinition> { ["children"] = child } };
        var captured = _factory.Capture(projection, _model, [], new("sink-v1"));
        child.InitialModelState["name"] = "changed-initial";
        captured.CanonicalContent.Contains("child-initial").ShouldBeTrue();
        _factory.Capture(projection, _model, [], new("sink-v1")).Revision.ShouldNotEqual(captured.Revision);
    }
    [Fact] void should_capture_exact_event_generations()
    {
        var from = new Dictionary<EventType, FromDefinition>
        {
            [new("event", 1)] = new(new Dictionary<PropertyPath, string>(), "id", null),
            [new("event", 2)] = new(new Dictionary<PropertyPath, string>(), "id", null)
        };
        var captured = _factory.Capture(_projection with { From = from }, _model, [], "sink-r1");
        var definitions = JsonNode.Parse(captured.CanonicalContent)!["Projection"]!["From"]!.AsObject();
        definitions.ContainsKey("event+1+False").ShouldBeTrue();
        definitions.ContainsKey("event+2+False").ShouldBeTrue();
    }
    [Fact] void should_capture_event_tombstone_status()
    {
        var from = new Dictionary<EventType, FromDefinition> { [new("event", 1, true)] = new(new Dictionary<PropertyPath, string>(), "id", null) };
        var captured = _factory.Capture(_projection with { From = from }, _model, [], "sink-r1");
        JsonNode.Parse(captured.CanonicalContent)!["Projection"]!["From"]!.AsObject().ContainsKey("event+1+True").ShouldBeTrue();
    }
    [Fact] void should_refuse_global_scope() => Catch.Exception(() => _factory.Capture(_projection with { Scope = ProjectionScope.Global }, _model, [], new("sink-v1"))).ShouldBeOfExactType<InvalidMaterializationExecution>();
}
