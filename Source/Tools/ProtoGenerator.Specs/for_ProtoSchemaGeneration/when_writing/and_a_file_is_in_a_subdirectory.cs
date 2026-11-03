// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaGeneration.when_writing;

public class and_a_file_is_in_a_subdirectory : given.an_output_directory
{
    readonly DateTime _timestamp = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    string _importPath;

    void Establish()
    {
        var directory = Path.Combine(_outputDirectory, "protobuf-net");
        Directory.CreateDirectory(directory);
        _importPath = Path.Combine(directory, "bcl.proto");
        File.WriteAllText(_importPath, "imported schema");
        File.SetLastWriteTimeUtc(_importPath, _timestamp);
    }

    void Because() => ProtoSchemaGeneration.Write(_schemas, _outputDirectory);

    [Fact] void should_preserve_the_content() => File.ReadAllText(_importPath).ShouldEqual("imported schema");
    [Fact] void should_preserve_the_timestamp() => File.GetLastWriteTimeUtc(_importPath).ShouldEqual(_timestamp);
}
