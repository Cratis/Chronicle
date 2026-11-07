// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes;

public abstract class and_a_property_under_a_cleared_object_is_set<THarness> : given.a_read_model_joined_on_a_source<THarness>
    where THarness : ISinkHarness, new()
{
    IDictionary<string, object?> _owner;

    async Task Establish()
    {
        var state = (ExpandoObject)(await Find("root"))!;
        Change[] changes = [new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference("owner", null, null)])];
        await _sink.ApplyChanges(new Key("root", ArrayIndexers.NoIndexers), Changeset(state, changes), 2UL);
    }

    async Task Because()
    {
        var state = (ExpandoObject)(await Find("root"))!;
        Change[] changes = [new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference("owner.name", null, "recreated")])];
        await _sink.ApplyChanges(new Key("root", ArrayIndexers.NoIndexers), Changeset(state, changes), 3UL);
        _owner = (IDictionary<string, object?>)(await Find("root"))!["owner"]!;
    }

    [Fact] public void should_recreate_the_object() => _owner["name"].ShouldEqual("recreated");
    [Fact] public void should_not_restore_the_old_nested_identity() => (_owner.TryGetValue("id", out var id) ? id : null).ShouldNotEqual("owner-1");
}
