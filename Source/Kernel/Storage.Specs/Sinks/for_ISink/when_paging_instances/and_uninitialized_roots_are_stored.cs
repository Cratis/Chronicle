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

public abstract class and_uninitialized_roots_are_stored<T> : given.a_populated_sink<T>
    where T : ISinkHarness, new()
{
    ReadModelInstances _page;
    ReadModelInstances _all;
    ExpandoObject? _placeholder;
    string[] _initialNames;
    string[] _createdNames;

    async Task Establish()
    {
        await WriteRoot("00-marker-only", false);
        await WriteRoot("00-with-data", false, "Joined before creation");
        await WriteRoot("f", true, "Initialized");
    }

    async Task Because()
    {
        _placeholder = await _sink.FindInPrimaryOrDefault("00-with-data");
        _page = await _sink.GetInstances(skip: 0, take: 2);
        _all = await _sink.GetInstances(take: 100);
        var initial = await _sink.ObserveInstances(take: 2).FirstAsync().ToTask().WaitAsync(TimeSpan.FromSeconds(10));
        _initialNames = Names(initial);
        var created = _sink.ObserveInstances(take: 2)
            .Where(instances => Names(instances).Contains("Created after the join"))
            .FirstAsync().ToTask();
        await WriteRoot("00-with-data", true, "Created after the join");
        _createdNames = Names(await created.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Fact] public void should_filter_before_paging() => Names(_page).ShouldEqual(["a", "b"]);
    [Fact] public void should_count_only_visible_roots() => _page.TotalCount.ShouldEqual(6L);
    [Fact] public void should_return_all_visible_roots() => Names(_all).ShouldEqual(["a", "b", "c", "d", "e", "Initialized"]);
    [Fact] public void should_observe_only_visible_roots_initially() => _initialNames.ShouldEqual(["a", "b"]);
    [Fact] public void should_observe_a_root_after_creation() => _createdNames.ShouldContain("Created after the join");
    [Fact] public void should_keep_the_placeholder_for_internal_lookups() => _placeholder.ShouldNotBeNull();
    [Fact] public void should_preserve_joined_data_for_internal_lookups() => ((IDictionary<string, object?>)_placeholder!)["name"].ShouldEqual("Joined before creation");

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
        await _sink.ApplyChanges(new Key(id, ArrayIndexers.NoIndexers), changes, initialized ? (EventSequenceNumber)2 : EventSequenceNumber.First);
    }
}
