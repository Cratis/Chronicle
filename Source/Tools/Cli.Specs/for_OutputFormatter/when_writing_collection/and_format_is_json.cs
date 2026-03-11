// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_OutputFormatter.when_writing_collection;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_json : Specification
{
    string _output;

    void Because()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        var data = new[] { new { Name = "Alice", Age = 30 }, new { Name = "Bob", Age = 25 } };
        OutputFormatter.Write("json", data, ["Name", "Age"], item => [item.Name, item.Age.ToString()]);

        _output = writer.ToString();
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
    }

    [Fact] void should_contain_alice() => _output.ShouldContain("Alice");
    [Fact] void should_contain_bob() => _output.ShouldContain("Bob");
    [Fact] void should_be_valid_json() => System.Text.Json.JsonDocument.Parse(_output).ShouldNotBeNull();
}
