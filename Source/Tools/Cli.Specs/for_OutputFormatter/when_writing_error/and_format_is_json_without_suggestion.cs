// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_OutputFormatter.when_writing_error;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_json_without_suggestion : Specification
{
    string _output;

    void Because()
    {
        var writer = new StringWriter();
        Console.SetError(writer);

        OutputFormatter.WriteError("json", "Connection lost");

        _output = writer.ToString();
        Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
    }

    [Fact] void should_contain_the_error() => _output.ShouldContain("Connection lost");
    [Fact] void should_not_contain_suggestion_key() => _output.ShouldNotContain("suggestion");
}
