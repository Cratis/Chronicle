// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding.when_encoding;

public class and_a_part_is_a_plain_object : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = "ok", ["b"] = new SomePoco() }));

    [Fact] void should_refuse_the_key() => _exception.ShouldBeOfExactType<NestedCompositeKeyPartsAreNotSupported>();
    [Fact] void should_name_the_part() => ((NestedCompositeKeyPartsAreNotSupported)_exception).Part.ShouldEqual("b");
}
