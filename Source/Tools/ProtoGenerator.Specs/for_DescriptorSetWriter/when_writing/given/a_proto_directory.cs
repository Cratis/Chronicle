// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_DescriptorSetWriter.when_writing.given;

public class a_proto_directory : Specification
{
    protected string _outputDirectory;
    protected string _protoPath;
    protected string _descriptorPath;

    void Establish()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_outputDirectory);
        _protoPath = Path.Combine(_outputDirectory, "example.proto");
        _descriptorPath = Path.Combine(_outputDirectory, DescriptorSetWriter.FileName);
        File.WriteAllText(_protoPath, "syntax = \"proto3\"; message Example { string value = 1; }");
    }

    void Destroy() => Directory.Delete(_outputDirectory, recursive: true);
}
