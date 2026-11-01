// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaGeneration.when_writing;

public class and_a_schema_changed : given.an_output_directory
{
    void Establish() => File.WriteAllText(_schemaPath, "old schema");

    void Because() => ProtoSchemaGeneration.Write(_schemas, _outputDirectory);

    [Fact] void should_update_the_content() => File.ReadAllText(_schemaPath).ShouldEqual("schema");
}
