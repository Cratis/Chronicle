// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_OutputFormatter.when_writing_object;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_json : Specification
{
    string _output;

    void Because()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        OutputFormatter.WriteObject(OutputFormats.Json, new { Status = "ok", Count = 42 });

        _output = writer.ToString();
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
    }

    [Fact] void should_contain_status() => _output.ShouldContain("ok");
    [Fact] void should_contain_count() => _output.ShouldContain("42");
    [Fact] void should_be_valid_json() => System.Text.Json.JsonDocument.Parse(_output).ShouldNotBeNull();
}
