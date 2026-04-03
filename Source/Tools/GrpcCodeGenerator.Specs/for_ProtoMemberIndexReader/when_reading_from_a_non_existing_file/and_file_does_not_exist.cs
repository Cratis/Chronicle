// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ProtoMemberIndexReader.when_reading_from_a_non_existing_file;

/// <summary>
/// Verifies that reading from a non-existing file returns an empty dictionary.
/// </summary>
public class and_file_does_not_exist : Specification
{
    IReadOnlyDictionary<string, int> _result = null!;

    void Because() =>
        _result = ProtoMemberIndexReader.ReadExistingIndexes(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "NonExistent.cs"),
            "SomeClass");

    [Fact] void should_return_empty_dictionary() => _result.ShouldBeEmpty();
}
