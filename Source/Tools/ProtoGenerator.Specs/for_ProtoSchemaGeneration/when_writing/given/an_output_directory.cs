// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaGeneration.when_writing.given;

public class an_output_directory : Specification
{
    protected string _outputDirectory;
    protected string _schemaPath;
    protected IReadOnlyDictionary<string, string> _schemas;

    void Establish()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_outputDirectory);
        _schemaPath = Path.Combine(_outputDirectory, "example.proto");
        _schemas = new Dictionary<string, string> { ["example"] = "schema" };
    }

    void Destroy() => Directory.Delete(_outputDirectory, recursive: true);
}
