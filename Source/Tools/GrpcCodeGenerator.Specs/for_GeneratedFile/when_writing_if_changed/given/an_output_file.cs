// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_GeneratedFile.when_writing_if_changed.given;

public class an_output_file : Specification
{
    protected string _outputDirectory;
    protected string _path;
    protected bool _written;

    void Establish()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_outputDirectory);
        _path = Path.Combine(_outputDirectory, "generated.cs");
    }

    void Destroy() => Directory.Delete(_outputDirectory, recursive: true);
}
