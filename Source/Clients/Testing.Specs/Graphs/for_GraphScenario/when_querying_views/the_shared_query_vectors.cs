// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Graphs;

namespace Cratis.Chronicle.Testing.Graphs.for_GraphScenario.when_querying_views;

public class the_shared_query_vectors : given.a_seeded_organization
{
    readonly Dictionary<string, string> _actual = [];
    readonly Dictionary<string, string> _expected = [];

    public static TheoryData<string> Cases => [.. Vectors["cases"]!.AsArray().Select(_ => _!["name"]!.GetValue<string>())];

    async Task Because()
    {
        foreach (var vector in Vectors["cases"]!.AsArray().Select(_ => _!.AsObject()))
        {
            var root = _scenario.Views.NodeOf(Vectors["graph"]!.GetValue<string>(), vector["view"]!.GetValue<string>());
            var single = vector["shape"]!.GetValue<string>() == "single";
            var query = new GraphQuery(
                Vectors["graph"]!.GetValue<string>(),
                single ? GraphQueryShape.First : GraphQueryShape.List,
                root,
                [.. vector["clauses"]!.AsArray().Select(_ => ClauseOf(root, _!))]);
            var rows = await _scenario.QueryExecutor.Execute(query);
            var actual = single ? JsonOf(rows.Count == 0 ? null : rows[0]) : new JsonArray([.. rows.Select(JsonOf)]);
            var name = vector["name"]!.GetValue<string>();
            _actual[name] = actual?.ToJsonString() ?? "null";
            _expected[name] = vector["expected"]?.ToJsonString() ?? "null";
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    void should_give_exactly_the_expected_result(string name) => _actual[name].ShouldEqual(_expected[name]);

    static GraphQueryClause ClauseOf(GraphQueryNode node, JsonNode clause)
    {
        var member = node.Members.Single(_ => _.Name == clause["member"]!.GetValue<string>());
        return clause["kind"]!.GetValue<string>() == "related"
            ? new GraphRelatedClause(member.Name, [.. clause["clauses"]!.AsArray().Select(_ => (GraphPropertyClause)ClauseOf(member.Relation!.Target, _!))])
            : new GraphPropertyClause(member.Name, member.Source!, ValueOf(clause["value"]));
    }

    static JsonNode? JsonOf(object? value) => value switch
    {
        null => null,
        GraphRow row => new JsonObject(row.Values.Select(_ => KeyValuePair.Create(_.Key, JsonOf(_.Value)))),
        string text => JsonValue.Create(text),
        IEnumerable<object?> items => new JsonArray([.. items.Select(JsonOf)]),
        _ => JsonValue.Create(value.ToString())
    };
}
