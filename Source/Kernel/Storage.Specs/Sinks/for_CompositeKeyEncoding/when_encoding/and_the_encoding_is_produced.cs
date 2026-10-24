// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding.when_encoding;

public class and_the_encoding_is_produced : Specification
{
    string _result;

    void Because() => _result = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = "one", ["b"] = "two" });

    [Fact] void should_start_with_the_version_marker() => _result.StartsWith("~k1:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_be_recognized_as_encoded() => CompositeKeyEncoding.IsEncoded(_result).ShouldBeTrue();
    [Fact] void should_use_length_prefixed_parts() => _result.ShouldEqual("~k1:3:one3:two");
    [Fact] void should_not_recognize_a_legacy_id_as_encoded() => CompositeKeyEncoding.IsEncoded("one_two").ShouldBeFalse();
}
