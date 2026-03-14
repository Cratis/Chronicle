// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_OutputFormatter.when_writing_collection;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_plain : Specification
{
    string _output;

    void Because()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        var data = new[] { new { Name = "Alice", Age = 30 } };
        OutputFormatter.Write(OutputFormats.Plain, data, ["Name", "Age"], item => [item.Name, item.Age.ToString()]);

        _output = writer.ToString();
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
    }

    [Fact] void should_contain_header_row() => _output.ShouldContain("Name\tAge");
    [Fact] void should_contain_data_row() => _output.ShouldContain("Alice\t30");
}
