// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaGeneration.when_writing;

public class and_a_schema_is_unchanged : given.an_output_directory
{
    readonly DateTime _timestamp = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    IReadOnlyList<string> _paths;

    void Establish()
    {
        File.WriteAllText(_schemaPath, "schema");
        File.SetLastWriteTimeUtc(_schemaPath, _timestamp);
    }

    void Because() => _paths = ProtoSchemaGeneration.Write(_schemas, _outputDirectory);

    [Fact] void should_preserve_the_timestamp() => File.GetLastWriteTimeUtc(_schemaPath).ShouldEqual(_timestamp);
    [Fact] void should_return_the_schema_path() => _paths.ShouldContainOnly(_schemaPath);
}
