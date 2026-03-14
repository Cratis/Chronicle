// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_OutputFormatter.when_writing_collection;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_json_compact : Specification
{
    string _output;

    void Because()
    {
        var data = new[] { new { Name = "Alice", Age = 30 }, new { Name = "Bob", Age = 25 } };
        var original = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            OutputFormatter.Write(OutputFormats.JsonCompact, data, ["Name", "Age"], item => [item.Name, item.Age.ToString()]);
        }
        finally
        {
            Console.SetOut(original);
        }

        _output = sw.ToString().Trim();
    }

    [Fact] void should_be_valid_json() => System.Text.Json.JsonDocument.Parse(_output);
    [Fact] void should_be_compact() => _output.ShouldNotContain("\n");
    [Fact] void should_contain_alice() => _output.ShouldContain("Alice");
    [Fact] void should_contain_bob() => _output.ShouldContain("Bob");
}
