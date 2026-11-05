// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes;

public abstract class and_a_join_maps_a_different_root_identity<THarness> : given.a_read_model_joined_on_a_source<THarness>
    where THarness : ISinkHarness, new()
{
    IDictionary<string, object?>? _root;
    IDictionary<string, object?>? _source;

    async Task Because()
    {
        var state = new ExpandoObject();
        Change[] changes =
        [
            new Joined(state, "source", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [
                new PropertiesChanged<ExpandoObject>(state, [
                    new PropertyDifference(new PropertyPath("id"), "root", "source"),
                    new PropertyDifference(new PropertyPath("name"), "before", "after")
                ])
            ])
        ];
        await _sink.ApplyChanges(new Key("source", ArrayIndexers.NoIndexers), Changeset(state, changes), 2UL);
        _root = await Find("root");
        _source = await Find("source");
    }

    [Fact] public void should_keep_the_row_under_its_identity() => _root.ShouldNotBeNull();
    [Fact] public void should_keep_the_identity() => _root!["id"].ShouldEqual("root");
    [Fact] public void should_apply_the_other_mapped_value() => _root!["name"].ShouldEqual("after");
    [Fact] public void should_not_create_a_row_for_the_mapped_identity() => _source.ShouldBeNull();
}
