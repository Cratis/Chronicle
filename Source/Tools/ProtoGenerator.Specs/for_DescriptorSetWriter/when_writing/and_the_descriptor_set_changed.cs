// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_DescriptorSetWriter.when_writing;

public class and_the_descriptor_set_changed : given.a_proto_directory
{
    byte[] _original;
    ProtoBuf.Reflection.Error[] _errors;

    void Establish()
    {
        DescriptorSetWriter.Write(_outputDirectory).ShouldBeEmpty();
        _original = File.ReadAllBytes(_descriptorPath);
        File.WriteAllText(_protoPath, "syntax = \"proto3\"; message Example { string value = 1; int32 count = 2; }");
    }

    void Because() => _errors = DescriptorSetWriter.Write(_outputDirectory);

    [Fact] void should_update_the_descriptor() => File.ReadAllBytes(_descriptorPath).SequenceEqual(_original).ShouldBeFalse();
    [Fact] void should_have_no_errors() => _errors.ShouldBeEmpty();
}
