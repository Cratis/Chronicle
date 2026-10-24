// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_finding_by_composite_key;

public class and_the_key_value_is_resolved : given.a_sink_with_composite_keys
{
    object _result;

    void Because() => _result = ((InMemorySink)_sink).GetKeyValue(CompositeKey(("b", "y_z"), ("a", null)));

    [Fact] void should_be_the_canonical_encoding() => _result.ShouldEqual("~k1:-3:y_z");
    [Fact] void should_carry_the_version_marker() => CompositeKeyEncoding.IsEncoded((string)_result).ShouldBeTrue();
}
