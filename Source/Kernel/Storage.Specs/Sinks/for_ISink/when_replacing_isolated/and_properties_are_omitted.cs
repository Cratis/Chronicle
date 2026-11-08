// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

public abstract class and_properties_are_omitted<THarness> : given.a_read_model_with_nested_values<THarness>
    where THarness : ISinkHarness, new()
{
    Task Because() => ReplaceWith(new ExpandoObject());

    [Fact] public void should_replace_the_row() => _result.ShouldEqual(IsolatedReplaceResult.Replaced);
    [Fact] public void should_clear_the_number() => Convert.ToInt64(StoredValue("count"), CultureInfo.InvariantCulture).ShouldEqual(0L);
    [Fact] public void should_clear_the_flag() => Convert.ToBoolean(StoredValue("enabled"), CultureInfo.InvariantCulture).ShouldBeFalse();
    [Fact] public void should_clear_the_scalar() => StoredValue("name").ShouldBeNull();
    [Fact] public void should_clear_the_nested_name() => string.IsNullOrEmpty(NestedValue("name") as string).ShouldBeTrue();
    [Fact] public void should_clear_the_nested_number() => Convert.ToInt64(NestedValue("age"), CultureInfo.InvariantCulture).ShouldEqual(0L);
    [Fact] public void should_clear_the_children() => (StoredValue("children") as IEnumerable<object> ?? []).ShouldBeEmpty();
    [Fact] public void should_keep_the_identity() => _stored["id"].ShouldEqual("root");
}
