// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_round_tripping_a_dotted_path_dictionary : given.a_dictionary_projection
{
    NestedDictionaryReadModel _result;

    void Establish() => _definition.From[_payloadReceived] = new(new Dictionary<PropertyPath, string> { ["details.payload"] = "nested.data" }, "$eventSourceId", null);

    async Task Because()
    {
        var projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);
        projection.OnNext(new ProjectionEventContext(_key, _incoming, _changeset, projection.GetOperationTypeFor(_payloadReceived), false));
        var json = _converter.ToJsonObject(_changeset.CurrentState, _schema).ToJsonString();
        _result = JsonSerializer.Deserialize<NestedDictionaryReadModel>(json, JsonSerializerOptions.Web)!;
    }

    [Fact] void should_deserialize_a_nested_dictionary() => _result.Details.Payload.ShouldBeOfExactType<Dictionary<string, object>>();
    [Fact] void should_preserve_the_single_member() => _result.Details.Payload.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_value_member() => ((JsonElement)_result.Details.Payload["value"]).GetString().ShouldEqual("kept");
}
