// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_carrying_content_between_generations;

public class and_inputs_and_generation_copies_are_independent : given.a_schema_filtered_chain
{
    ExpandoObject _compliant;
    JsonObject _sourceSnapshot;
    string _schemaSnapshot;
    JsonObject[] _operationSnapshots;

    void Establish()
    {
        _source["extra"] = "caller only";
        _sourceSnapshot = (JsonObject)_source.DeepClone();
        _schemaSnapshot = _definition.Generations.First().Schema.ToJson();
        _operationSnapshots = _definition.Migrations.Select(migration => (JsonObject)migration.UpcastJmesPath.DeepClone()).ToArray();
        _compliant = new();
        Members(_compliant)["marker"] = "compliant";
    }

    async Task Because()
    {
        _result = await _migrations.MigrateToAllGenerations(_definition, _type, _source, _compliant);
        Members(_result[3])["id"] = "changed";
    }

    [Fact] void should_not_modify_the_callers_json() => JsonNode.DeepEquals(_source, _sourceSnapshot).ShouldBeTrue();
    [Fact] void should_not_modify_the_compliant_content() => Members(_compliant)["marker"].ShouldEqual("compliant");
    [Fact] void should_keep_earlier_generation_copies_independent() => Members(_result[2])["id"].ShouldEqual("1");
    [Fact] void should_filter_the_source_before_the_first_edge() => Members(_result[1]).ContainsKey("extra").ShouldBeFalse();
    [Fact] void should_not_modify_the_schema() => _definition.Generations.First().Schema.ToJson().ShouldEqual(_schemaSnapshot);
    [Fact] void should_not_modify_operations() => _definition.Migrations.Select((migration, index) => JsonNode.DeepEquals(migration.UpcastJmesPath, _operationSnapshots[index])).All(equal => equal).ShouldBeTrue();
}
