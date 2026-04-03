// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ProtoMemberIndexReader.when_reading_from_source_with_proto_members;

/// <summary>
/// Verifies that existing ProtoMember indexes are correctly read from source with two indexed properties.
/// </summary>
public class and_class_has_two_members : given.a_source_with_proto_members
{
    IReadOnlyDictionary<string, int> _result = null!;

    void Because() =>
        _result = ProtoMemberIndexReader.ReadExistingIndexesFromSource(_source, ClassName);

    [Fact] void should_return_two_entries() => _result.Count.ShouldEqual(2);
    [Fact] void should_have_id_at_index_one() => _result["Id"].ShouldEqual(1);
    [Fact] void should_have_name_at_index_two() => _result["Name"].ShouldEqual(2);
}
