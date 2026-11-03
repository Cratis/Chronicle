// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_DescriptorSetWriter.when_writing;

public class and_the_descriptor_set_is_unchanged : given.a_proto_directory
{
    readonly DateTime _timestamp = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    ProtoBuf.Reflection.Error[] _errors;

    void Establish()
    {
        DescriptorSetWriter.Write(_outputDirectory).ShouldBeEmpty();
        File.SetLastWriteTimeUtc(_descriptorPath, _timestamp);
    }

    void Because() => _errors = DescriptorSetWriter.Write(_outputDirectory);

    [Fact] void should_preserve_the_timestamp() => File.GetLastWriteTimeUtc(_descriptorPath).ShouldEqual(_timestamp);
    [Fact] void should_have_no_errors() => _errors.ShouldBeEmpty();
}
