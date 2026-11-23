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

public class when_looking_up_a_child_within_a_root : Specification
{
    readonly SqlReadCapture _capture = new();
    SqlSinkHarness _harness;
    ISink _sink;
    object _matched;
    int _rows;
    bool _missingRoot;
    bool _missingChild;

    async Task Establish()
    {
        _harness = new SqlSinkHarness { Interceptors = [_capture] };
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"children":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"}}}}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("scoped", "scoped_rows", "Scoped", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "scoped", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        await AddRoot("root-a", "shared", "only-first");
        await AddRoot("root-b", "shared");
    }

    async Task Because()
    {
        _capture.Clear();
        var result = await _sink.TryFindRootKeyByChildValueInRoot("[children].id", "shared", Root("root-b"));
        result.TryGetValue(out var key).ShouldBeTrue();
        _matched = key.Value;
        _rows = _capture.Rows;
        _missingRoot = (await _sink.TryFindRootKeyByChildValueInRoot("[children].id", "shared", Root("missing"))).HasValue;
        _missingChild = (await _sink.TryFindRootKeyByChildValueInRoot("[children].id", "only-first", Root("root-b"))).HasValue;
    }

    async Task AddRoot(string root, params string[] children)
    {
        var state = children.Select(child =>
        {
            dynamic item = new ExpandoObject();
            item.id = child;
            return (ExpandoObject)item;
        }).ToList();
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("children", null, state)])]);
        await _sink.ApplyChanges(Root(root), changeset, 1UL);
    }

    static Key Root(string id) => new(id, ArrayIndexers.NoIndexers);
    void Destroy() => _harness.Dispose();

    [Fact] void should_resolve_the_requested_root_with_a_repeated_child_key() => _matched.ShouldEqual("root-b");
    [Fact] void should_filter_the_root_before_materializing_candidates() => _rows.ShouldEqual(1);
    [Fact] void should_not_find_a_child_in_a_missing_root() => _missingRoot.ShouldBeFalse();
    [Fact] void should_not_select_a_child_from_another_root() => _missingChild.ShouldBeFalse();
}
