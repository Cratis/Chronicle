// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances;

public abstract class and_child_only_roots_are_stored_uninitialized<T> : given.a_populated_sink<T>
    where T : ISinkHarness, new()
{
    ReadModelInstances _page;
    ReadModelInstances _all;
    ReadModelInstances _default;
    string[] _observedKeys;
    ExpandoObject? _removed;

    async Task Establish()
    {
        await WriteRoot("00-legacy-marker-only", false);
        await WriteRoot("00-legacy-with-data", false, "Child-only before upgrade");
        await WriteRoot("f", true, "Initialized");
        await WriteRoot("00-removed", false);
        var state = new ExpandoObject();
        var removal = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        removal.InitialState.Returns(state);
        removal.Changes.Returns([new Removed(state)]);
        removal.HasBeenRemoved().Returns(true);
        await _sink.ApplyChanges(new Key("00-removed", ArrayIndexers.NoIndexers), removal, 2UL);
    }

    async Task Because()
    {
        _page = await _sink.GetInstances(null, 0, 2, ReadModelInstanceVisibility.IncludeUninitialized);
        _all = await _sink.GetInstances(null, 0, 100, ReadModelInstanceVisibility.IncludeUninitialized);
        var observed = await _sink.ObserveInstances(null, 0, 2, ReadModelInstanceVisibility.IncludeUninitialized).FirstAsync().ToTask().WaitAsync(TimeSpan.FromSeconds(10));
        _observedKeys = Keys(observed);
        _default = await _sink.GetInstances(take: 100);
        _removed = await _sink.FindInPrimaryOrDefault("00-removed");
    }

    [Fact] public void should_count_all_child_only_roots() => _all.TotalCount.ShouldEqual(8L);
    [Fact] public void should_page_the_legacy_roots_first() => Keys(_page.Instances).ShouldEqual(["00-legacy-marker-only", "00-legacy-with-data"]);
    [Fact] public void should_return_the_legacy_root_with_data() => _all.Instances.Any(instance => ((IDictionary<string, object?>)instance).TryGetValue("name", out var name) && name as string == "Child-only before upgrade").ShouldBeTrue();
    [Fact] public void should_observe_the_same_page() => _observedKeys.ShouldEqual(Keys(_page.Instances));
    [Fact] public void should_hide_uninitialized_roots_by_default() => _default.TotalCount.ShouldEqual(6L);
    [Fact] public void should_not_return_the_removed_root() => Keys(_all.Instances).ShouldNotContain("00-removed");
    [Fact] public void should_not_return_the_removed_root_by_default() => Keys(_default.Instances).ShouldNotContain("00-removed");
    [Fact] public void should_remove_the_root_from_storage() => _removed.ShouldBeNull();

    static string[] Keys(IEnumerable<ExpandoObject> instances) => instances.Select(instance => (string)((IDictionary<string, object?>)instance)["id"]!).ToArray();

    async Task WriteRoot(string id, bool initialized, string? name = null)
    {
        var state = new ExpandoObject();
        var properties = new List<PropertyDifference> { new(new PropertyPath(WellKnownProperties.ReadModelInstanceInitialized), null, initialized) };
        if (name is not null)
        {
            properties.Add(new(new PropertyPath("name"), null, name));
        }
        var changes = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changes.InitialState.Returns(state);
        changes.Changes.Returns([new PropertiesChanged<ExpandoObject>(state, properties)]);
        await _sink.ApplyChanges(new Key(id, ArrayIndexers.NoIndexers), changes, EventSequenceNumber.First);
    }
}
