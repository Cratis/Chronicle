// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelDefinitions.when_comparing;

public class and_any_persisted_property_changes : Specification
{
    ReadModelDefinition _original;
    Dictionary<string, bool> _results;

    void Establish() => _original = new(
        "model",
        "container",
        "Model",
        ReadModelOwner.None,
        ReadModelSource.Unknown,
        ReadModelObserverType.NotSet,
        ReadModelObserverIdentifier.Unspecified,
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, JsonSchema> { [(ReadModelGeneration)1] = JsonSchema.FromJson("{\"type\":\"object\"}") },
        [new IndexDefinition("name")]);

    void Because()
    {
        var changes = new Dictionary<string, ReadModelDefinition>
        {
            ["identifier"] = _original with { Identifier = "other" },
            ["container"] = _original with { ContainerName = "other" },
            ["display"] = _original with { DisplayName = "Other" },
            ["owner"] = _original with { Owner = ReadModelOwner.Client },
            ["source"] = _original with { Source = ReadModelSource.Code },
            ["observerType"] = _original with { ObserverType = ReadModelObserverType.Projection },
            ["observer"] = _original with { ObserverIdentifier = "other" },
            ["sinkConfiguration"] = _original with { Sink = new SinkDefinition(new SinkConfigurationId(Guid.Parse("1cb764e8-15f8-4104-bf47-df058718dfe3")), SinkTypeId.None) },
            ["sinkType"] = _original with { Sink = new SinkDefinition(SinkConfigurationId.None, "other") },
            ["indexes"] = _original with { Indexes = [new IndexDefinition("other")] },
            ["schema"] = _original with { Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [(ReadModelGeneration)1] = JsonSchema.FromJson("{\"type\":\"string\"}") } },
            ["generation"] = _original with { Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [(ReadModelGeneration)2] = JsonSchema.FromJson("{\"type\":\"object\"}") } }
        };
        _results = changes.ToDictionary(pair => pair.Key, pair => ReadModelDefinitions.AreEqual(_original, pair.Value));
    }

    [Fact] void should_detect_identifier_changes() => _results["identifier"].ShouldBeFalse();
    [Fact] void should_detect_container_changes() => _results["container"].ShouldBeFalse();
    [Fact] void should_detect_display_name_changes() => _results["display"].ShouldBeFalse();
    [Fact] void should_detect_owner_changes() => _results["owner"].ShouldBeFalse();
    [Fact] void should_detect_source_changes() => _results["source"].ShouldBeFalse();
    [Fact] void should_detect_observer_type_changes() => _results["observerType"].ShouldBeFalse();
    [Fact] void should_detect_observer_identifier_changes() => _results["observer"].ShouldBeFalse();
    [Fact] void should_detect_sink_configuration_changes() => _results["sinkConfiguration"].ShouldBeFalse();
    [Fact] void should_detect_sink_type_changes() => _results["sinkType"].ShouldBeFalse();
    [Fact] void should_detect_index_changes() => _results["indexes"].ShouldBeFalse();
    [Fact] void should_detect_schema_changes() => _results["schema"].ShouldBeFalse();
    [Fact] void should_detect_generation_changes() => _results["generation"].ShouldBeFalse();
}
