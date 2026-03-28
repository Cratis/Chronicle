// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ProtoMemberIndexReader.when_reading_from_source_without_target_class;

/// <summary>
/// Verifies that reading a class that does not exist in the source returns an empty dictionary.
/// </summary>
public class and_class_is_missing : Specification
{
    IReadOnlyDictionary<string, int> _result = null!;

    void Because() =>
        _result = ProtoMemberIndexReader.ReadExistingIndexesFromSource(
            "namespace Foo; public class Bar { public int X { get; set; } }",
            "NonExistentClass");

    [Fact] void should_return_empty_dictionary() => _result.ShouldBeEmpty();
}
