// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

public abstract class and_nullable_properties_are_explicitly_null<THarness> : given.a_read_model_with_nested_values<THarness>
    where THarness : ISinkHarness, new()
{
    async Task Because()
    {
        var state = new ExpandoObject();
        var values = (IDictionary<string, object?>)state;
        values["name"] = null;
        values["child"] = null;
        values["children"] = null;
        await ReplaceWith(state);
    }

    [Fact] public void should_replace_the_row() => _result.ShouldEqual(IsolatedReplaceResult.Replaced);
    [Fact] public void should_preserve_the_scalar_null() => StoredValue("name").ShouldBeNull();
    [Fact] public void should_preserve_the_object_null() => StoredValue("child").ShouldBeNull();
    [Fact] public void should_not_retain_any_array_elements() =>
        (StoredValue("children") is null || (StoredValue("children") is System.Collections.IEnumerable children && !children.Cast<object>().Any())).ShouldBeTrue();
    [Fact] public void should_keep_the_identity() => _stored["id"].ShouldEqual("root");
}
