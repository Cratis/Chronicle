// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public abstract class when_filtering_join_candidates : Specification
{
    const string Target = "quoted'\"\\snowman☃";
    static readonly Guid _foreignId = Guid.Parse("ac0ef349-d07d-4a70-af32-791a23883df4");
    ISqlSinkHarness _harness;
    ISink _sink;
    readonly SqlReadCapture _capture = new();
    int _lookupRows;
    bool _hasJsonPredicate;
    bool _hasScalarPredicate;
    IDictionary<string, object?> _matched;
    IDictionary<string, object?> _unrelated;
    bool _found;
    string _guidJoinName;
    string _afterIncompatibleJoin;
    protected abstract ISqlSinkHarness CreateHarness(SqlReadCapture capture);

    async Task Establish()
    {
        _harness = CreateHarness(_capture);
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"group":{"type":"integer"},"foreignId":{"type":"string"},"name":{"type":"string"},"children":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"label":{"type":"string"}}}}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("candidates", "candidate_rows", "Candidates", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "candidates", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        for (var index = 0; index < 30; index++)
        {
            dynamic child = new ExpandoObject();
            child.id = index < 2 ? Target : $"child-{index}";
            child.label = index == 2 ? Target : "label";
            await Apply(index.ToString(), new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("group", null, index < 2 ? 77 : index), new PropertyDifference("foreignId", null, _foreignId.ToString()), new PropertyDifference("name", null, "initial"), new PropertyDifference("children", null, new List<ExpandoObject> { child })]), 1UL);
        }
    }

    async Task Because()
    {
        _capture.Clear();
        var found = await _sink.TryFindRootKeyByChildValue("[children].id", Target);
        _found = found.TryGetValue(out var key) && (key.Value?.ToString() == "0" || key.Value?.ToString() == "1");
        _lookupRows = _capture.Rows;
        _hasJsonPredicate = _capture.Commands.Any(command => command.Contains("WHERE", StringComparison.OrdinalIgnoreCase) && (command.Contains("json_tree", StringComparison.OrdinalIgnoreCase) || command.Contains("jsonb_path_exists", StringComparison.OrdinalIgnoreCase) || command.Contains("OPENJSON", StringComparison.OrdinalIgnoreCase)));
        _capture.Clear();
        await Apply("0", new Joined(new ExpandoObject(), new Group(77), "group", ArrayIndexers.NoIndexers, [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, "joined")])]), 2UL);
        _hasScalarPredicate = _capture.Commands.Any(command => command.Contains("WHERE", StringComparison.OrdinalIgnoreCase) && command[(command.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase) + 5)..].Contains("group", StringComparison.Ordinal));
        await Apply("0", new ChildRemovedFromAll("children", "id", Target, ArrayIndexers.NoIndexers), 3UL);
        _matched = (await _sink.FindOrDefault(new Key("1", ArrayIndexers.NoIndexers)))!;
        _unrelated = (await _sink.FindOrDefault(new Key("2", ArrayIndexers.NoIndexers)))!;
        await Apply("0", new Joined(new ExpandoObject(), _foreignId, "foreignId", ArrayIndexers.NoIndexers, [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, "guid-joined")])]), 4UL);
        var guidParent = (IDictionary<string, object?>)(await _sink.FindOrDefault(new Key("1", ArrayIndexers.NoIndexers)))!;
        _guidJoinName = (string)guidParent["name"]!;
        await Apply("0", new Joined(new ExpandoObject(), _foreignId, "group", ArrayIndexers.NoIndexers, [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, "incompatible")])]), 5UL);
        var afterIncompatible = (IDictionary<string, object?>)(await _sink.FindOrDefault(new Key("1", ArrayIndexers.NoIndexers)))!;
        _afterIncompatibleJoin = (string)afterIncompatible["name"]!;
    }

    async Task Apply(string id, Change change, ulong sequence)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([change]);
        await _sink.ApplyChanges(new Key(id, ArrayIndexers.NoIndexers), changeset, sequence);
    }

    void Destroy() => _harness.Dispose();
    [Fact] public void should_join_a_guid_key_against_a_string_column() => _guidJoinName.ShouldEqual("guid-joined");
    [Fact] public void should_fall_back_without_throwing_for_an_incompatible_scalar_key() => _afterIncompatibleJoin.ShouldEqual("guid-joined");
    [Fact] public void should_find_a_parent_with_escaped_unicode_child_values() => _found.ShouldBeTrue();
    [Fact] public void should_filter_json_candidates_before_materializing() => _hasJsonPredicate.ShouldBeTrue();
    [Fact] public void should_materialize_only_the_three_matching_json_candidates() => _lookupRows.ShouldEqual(3);
    [Fact] public void should_push_scalar_concept_joins_into_sql() => _hasScalarPredicate.ShouldBeTrue();
    [Fact] public void should_update_every_matching_parent() => _matched["name"].ShouldEqual("joined");
    [Fact] public void should_remove_matching_children_from_every_parent() => ((IEnumerable<object>)_matched["children"]!).ShouldBeEmpty();
    [Fact] public void should_not_update_a_nonmatching_parent() => _unrelated["name"].ShouldEqual("initial");
    [Fact] public void should_recheck_the_path_after_the_coarse_json_filter() => ((IEnumerable<object>)_unrelated["children"]!).Count().ShouldEqual(1);

    record Group(int Value) : ConceptAs<int>(Value);
}
