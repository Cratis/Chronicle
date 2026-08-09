// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_all_instances;

public class and_read_model_is_materialized : given.all_dependencies
{
    GetAllInstancesResponse _result = null!;

    void Establish()
    {
        _readModelDefinition = _readModelDefinition with
        {
            Sink = new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.InMemory)
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);

        _sink.GetInstances(Arg.Any<ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ReadModelInstances([InstanceNamed("First"), InstanceNamed("Second")], 2));

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<Schemas.JsonSchema>())
            .Returns(call =>
            {
                var jsonObject = new JsonObject();
                foreach (var (key, value) in (IDictionary<string, object?>)call.Arg<ExpandoObject>())
                {
                    jsonObject[key] = value?.ToString();
                }

                return jsonObject;
            });
    }

    async Task Because() => _result = await _service.GetAllInstances(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        EventCount = ulong.MaxValue
    });

    [Fact] void should_return_every_materialized_instance() => _result.Instances.Count.ShouldEqual(2);
    [Fact] void should_return_the_first_instance() => NameOf(_result.Instances[0]).ShouldEqual("First");
    [Fact] void should_return_the_second_instance() => NameOf(_result.Instances[1]).ShouldEqual("Second");
    [Fact] void should_not_process_any_events() => _result.ProcessedEventsCount.ShouldEqual(0UL);

    static string? NameOf(string json) => JsonSerializer.Deserialize<JsonElement>(json).GetProperty("name").GetString();

    static ExpandoObject InstanceNamed(string name)
    {
        var instance = new ExpandoObject();
        ((IDictionary<string, object?>)instance)["name"] = name;
        return instance;
    }
}
