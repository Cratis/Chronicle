// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_OutputFormatter.when_writing_error;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_json : Specification
{
    string _output;

    void Because()
    {
        var writer = new StringWriter();
        Console.SetError(writer);

        OutputFormatter.WriteError("json", "Something failed", "Try again");

        _output = writer.ToString();
        Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
    }

    [Fact] void should_contain_the_error() => _output.ShouldContain("Something failed");
    [Fact] void should_contain_the_suggestion() => _output.ShouldContain("Try again");
    [Fact] void should_be_valid_json() => System.Text.Json.JsonDocument.Parse(_output).ShouldNotBeNull();
}
