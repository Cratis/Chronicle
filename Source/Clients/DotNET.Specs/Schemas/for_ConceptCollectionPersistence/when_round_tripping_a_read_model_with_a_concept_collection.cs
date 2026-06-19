// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_ConceptCollectionPersistence;

public class when_round_tripping_a_read_model_with_a_concept_collection : given.a_schema_driven_round_trip
{
    record Requirement(string Value) : ConceptAs<string>(Value);
    record CandidateItem(string Name, int Score);
    record Surface(
        Guid Id,
        IReadOnlyList<Requirement> Requirements,
        IEnumerable<CandidateItem> Candidates);

    IDictionary<string, object?> _result;

    void Because()
    {
        var instance = new Surface(
            Guid.NewGuid(),
            [new Requirement("React"), new Requirement("TypeScript")],
            [new CandidateItem("Alice", 10)]);

        var schema = _generator.Generate(typeof(Surface));
        var json = JsonSerializer.Serialize(instance, _clientOptions);
        _result = _converter.ToExpandoObject(JsonNode.Parse(json)!.AsObject(), schema);
    }

    [Fact] void should_keep_the_concept_collection_property() => _result.ContainsKey("Requirements").ShouldBeTrue();
    [Fact] void should_preserve_all_concept_collection_elements() => ((IEnumerable)_result["Requirements"]!).Cast<object>().Select(_ => _.ToString()).ShouldContainOnly(["React", "TypeScript"]);
    [Fact] void should_keep_the_record_collection_property() => _result.ContainsKey("Candidates").ShouldBeTrue();
    [Fact] void should_preserve_all_record_collection_elements() => ((IEnumerable)_result["Candidates"]!).Cast<ExpandoObject>().Count().ShouldEqual(1);
}
