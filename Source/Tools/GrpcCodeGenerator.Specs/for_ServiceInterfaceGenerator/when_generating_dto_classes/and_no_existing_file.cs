// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_dto_classes;

/// <summary>
/// Verifies that when generating with no pre-existing file, ProtoMember indexes start at 1 and are sequential.
/// </summary>
public class and_no_existing_file : given.a_generated_service_interface_with_output_dir
{
    string _generatedCode = null!;
    IReadOnlyDictionary<string, int> _registerProductRequestIndexes = null!;

    void Because()
    {
        _generatedCode = _generator.Generate(_serviceDefinition, _outputDir);

        _registerProductRequestIndexes = ProtoMemberIndexReader.ReadExistingIndexesFromSource(
            _generatedCode, "RegisterProductRequest");
    }

    [Fact] void should_have_proto_member_indexes() => _registerProductRequestIndexes.ShouldNotBeEmpty();
    [Fact] void should_start_indexes_at_one() => _registerProductRequestIndexes.Values.Min().ShouldEqual(1);
    [Fact] void should_have_sequential_indexes() =>
        _registerProductRequestIndexes.Values.Order().ShouldContainOnly(
            Enumerable.Range(1, _registerProductRequestIndexes.Count).ToArray());
}
