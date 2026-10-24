// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.for_GenerationExecutionSpecifications;

public class when_canonicalizing_nested_content : Specification
{
    string _result;
    void Because() => _result = GenerationExecutionSpecifications.Canonicalize("{\"z\":[{\"b\":2,\"a\":1},3],\"a\":{\"y\":2,\"x\":1}}");
    [Fact] void should_recursively_sort_object_keys() => _result.ShouldEqual("{\"a\":{\"x\":1,\"y\":2},\"z\":[{\"a\":1,\"b\":2},3]}");
    [Fact] void should_be_idempotent() => GenerationExecutionSpecifications.Canonicalize(_result).ShouldEqual(_result);
    [Fact] void should_preserve_array_order() => GenerationExecutionSpecifications.Canonicalize("{\"a\":[2,1]}").ShouldEqual("{\"a\":[2,1]}");
}
