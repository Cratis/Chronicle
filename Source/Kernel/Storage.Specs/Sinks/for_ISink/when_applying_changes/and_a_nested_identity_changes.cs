// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes;

public abstract class and_a_nested_identity_changes<THarness> : given.a_read_model_joined_on_a_source<THarness>
    where THarness : ISinkHarness, new()
{
    IDictionary<string, object?> _root;

    async Task Because()
    {
        var state = (ExpandoObject)(await Find("root"))!;
        Change[] changes =
        [
            new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference(new PropertyPath("owner.id"), "owner-1", "owner-2")])
        ];
        await _sink.ApplyChanges(new Key("root", ArrayIndexers.NoIndexers), Changeset(state, changes), 2UL);
        _root = (await Find("root"))!;
    }

    [Fact] public void should_apply_the_nested_identity() => ((IDictionary<string, object?>)_root["owner"]!)["id"].ShouldEqual("owner-2");
    [Fact] public void should_keep_the_root_identity() => _root["id"].ShouldEqual("root");
}
