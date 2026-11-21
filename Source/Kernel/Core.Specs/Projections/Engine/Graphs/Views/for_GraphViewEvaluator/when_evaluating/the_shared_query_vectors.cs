// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewEvaluator.when_evaluating;

public class the_shared_query_vectors : given.a_seeded_organization
{
    readonly Dictionary<string, string> _actual = [];
    readonly Dictionary<string, string> _expected = [];

    public static TheoryData<string> Cases => [.. Vectors["cases"]!.AsArray().Select(_ => _!["name"]!.GetValue<string>())];

    async Task Because()
    {
        foreach (var vector in Vectors["cases"]!.AsArray().Select(_ => _!.AsObject()))
        {
            var single = vector["shape"]!.GetValue<string>() == "single";
            var query = new GraphViewQuery(
                vector["view"]!.GetValue<string>(),
                [.. vector["clauses"]!.AsArray().Select(_ => ClauseOf(_!))],
                single);
            var rows = await GraphViewEvaluator.Evaluate(_sink, _graph, _views, query);
            var actual = single ? JsonOf(rows.Count == 0 ? null : rows[0]) : new JsonArray([.. rows.Select(JsonOf)]);
            var name = vector["name"]!.GetValue<string>();
            _actual[name] = actual?.ToJsonString() ?? "null";
            _expected[name] = vector["expected"]?.ToJsonString() ?? "null";
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    void should_give_exactly_the_expected_result(string name) => _actual[name].ShouldEqual(_expected[name]);

    static GraphViewClause ClauseOf(JsonNode clause) =>
        clause["kind"]!.GetValue<string>() == "related"
            ? new GraphViewRelatedClause(clause["member"]!.GetValue<string>(), [.. clause["clauses"]!.AsArray().Select(_ => (GraphViewPropertyClause)ClauseOf(_!))])
            : new GraphViewPropertyClause(clause["member"]!.GetValue<string>(), ValueOf(clause["value"]));

    static JsonNode? JsonOf(object? value) => value switch
    {
        null => null,
        GraphViewRow row => new JsonObject(row.Values.Select(_ => KeyValuePair.Create(_.Key, JsonOf(_.Value)))),
        string text => JsonValue.Create(text),
        IEnumerable<object?> items => new JsonArray([.. items.Select(JsonOf)]),
        _ => JsonValue.Create(value.ToString())
    };
}
