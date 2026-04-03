// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_dto_classes;

/// <summary>
/// Verifies that existing ProtoMember indexes are preserved when regenerating from an existing file.
/// </summary>
public class and_existing_file_has_proto_members : given.a_generated_service_interface_with_output_dir
{
    IReadOnlyDictionary<string, int> _firstGenerationIndexes = null!;
    IReadOnlyDictionary<string, int> _secondGenerationIndexes = null!;

    void Because()
    {
        var firstCode = _generator.Generate(_serviceDefinition, _outputDir);
        _firstGenerationIndexes = ProtoMemberIndexReader.ReadExistingIndexesFromSource(
            firstCode, "RegisterProductRequest");

        var secondCode = _generator.Generate(_serviceDefinition, _outputDir);
        _secondGenerationIndexes = ProtoMemberIndexReader.ReadExistingIndexesFromSource(
            secondCode, "RegisterProductRequest");
    }

    [Fact] void should_preserve_all_indexes() =>
        _secondGenerationIndexes.ShouldContainOnly(_firstGenerationIndexes);
}
