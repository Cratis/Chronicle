// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaGeneration.when_writing;

public class and_a_proto_is_no_longer_generated : given.an_output_directory
{
    string _stalePath;
    string _staleDescriptorPath;
    string _descriptorPath;

    void Establish()
    {
        _stalePath = Path.Combine(_outputDirectory, "stale.proto");
        _staleDescriptorPath = Path.Combine(_outputDirectory, "stale.desc");
        _descriptorPath = Path.Combine(_outputDirectory, DescriptorSetWriter.FileName);
        File.WriteAllText(_stalePath, "stale");
        File.WriteAllText(_staleDescriptorPath, "stale");
        File.WriteAllText(_descriptorPath, "descriptor");
    }

    void Because() => ProtoSchemaGeneration.Write(_schemas, _outputDirectory);

    [Fact] void should_delete_the_stale_proto() => File.Exists(_stalePath).ShouldBeFalse();
    [Fact] void should_delete_the_stale_descriptor() => File.Exists(_staleDescriptorPath).ShouldBeFalse();
    [Fact] void should_preserve_the_canonical_descriptor() => File.ReadAllText(_descriptorPath).ShouldEqual("descriptor");
}
