// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_OutputFormatter.when_writing_message;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_json : Specification
{
    string _output;

    void Because()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        OutputFormatter.WriteMessage(OutputFormats.Json, "Operation completed");

        _output = writer.ToString();
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
    }

    [Fact] void should_contain_the_message() => _output.ShouldContain("Operation completed");
    [Fact] void should_be_valid_json() => System.Text.Json.JsonDocument.Parse(_output).ShouldNotBeNull();
}
