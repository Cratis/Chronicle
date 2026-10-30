// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_finding_by_key;

public class and_instances_have_initialization_markers : given.a_materialized_read_model
{
    readonly List<ExpandoObject?> _results = [];
    ExpandoObject[] _stored;

    void Establish()
    {
        var markerOnly = new ExpandoObject();
        ((IDictionary<string, object?>)markerOnly)[WellKnownProperties.ReadModelInstanceInitialized] = false;
        _stored = [markerOnly, InstanceWithMarker(false), InstanceWithMarker(true), InstanceNamed("Legacy"), InstanceWithMarker("false"), InstanceWithMarker(null), InstanceWithMarker(0)];
    }

    async Task Because()
    {
        foreach (var instance in _stored)
        {
            _sink.FindInPrimaryOrDefault("the-key").Returns(instance);
            _results.Add(await _store.FindByKey(EventStore, EventStoreNamespace, _definition, "the-key"));
        }
    }

    [Fact] void should_hide_a_marker_only_placeholder() => _results[0].ShouldBeNull();
    [Fact] void should_hide_a_placeholder_with_joined_data() => _results[1].ShouldBeNull();
    [Fact] void should_return_an_initialized_root() => _results[2].ShouldEqual(_stored[2]);
    [Fact] void should_return_a_legacy_root_without_a_marker() => _results[3].ShouldEqual(_stored[3]);
    [Fact] void should_not_treat_a_string_as_a_boolean_marker() => _results[4].ShouldEqual(_stored[4]);
    [Fact] void should_not_treat_null_as_a_boolean_marker() => _results[5].ShouldEqual(_stored[5]);
    [Fact] void should_not_treat_zero_as_a_boolean_marker() => _results[6].ShouldEqual(_stored[6]);
    [Fact] void should_not_modify_internal_state() => ((IDictionary<string, object?>)_stored[1])[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(false);

    static ExpandoObject InstanceWithMarker(object? initialized)
    {
        var instance = InstanceNamed("Joined data");
        ((IDictionary<string, object?>)instance)[WellKnownProperties.ReadModelInstanceInitialized] = initialized;
        return instance;
    }
}
