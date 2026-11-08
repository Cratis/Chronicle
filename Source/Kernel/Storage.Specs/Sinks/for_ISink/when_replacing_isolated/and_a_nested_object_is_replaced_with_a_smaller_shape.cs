// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

public abstract class and_a_nested_object_is_replaced_with_a_smaller_shape<THarness> : given.a_read_model_with_nested_values<THarness>
    where THarness : ISinkHarness, new()
{
    async Task Because()
    {
        dynamic child = new ExpandoObject();
        child.name = "after";
        dynamic state = new ExpandoObject();
        state.child = (ExpandoObject)child;
        await ReplaceWith((ExpandoObject)state);
    }

    [Fact] public void should_replace_the_row() => _result.ShouldEqual(IsolatedReplaceResult.Replaced);
    [Fact] public void should_apply_the_nested_value() => NestedValue("name").ShouldEqual("after");
    [Fact] public void should_clear_the_omitted_nested_value() => Convert.ToInt64(NestedValue("age"), CultureInfo.InvariantCulture).ShouldEqual(0L);
    [Fact] public void should_keep_the_identity() => _stored["id"].ShouldEqual("root");
}
