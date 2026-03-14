// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;

namespace Cratis.Chronicle.Integration.Cli.for_Context;

[Collection(ChronicleCollection.Name)]
public class when_creating_context : Specification
{
    CliCommandResult _createResult = null!;
    CliCommandResult _deleteResult = null!;

    async Task Because()
    {
        _createResult = await CliCommandRunner.RunAsync(
            "context",
            "create",
            "integration-test-ctx",
            "--server",
            "chronicle://localhost:35000/?disableTls=true",
            "--output",
            "json");

        // Clean up the created context.
        _deleteResult = await CliCommandRunner.RunAsync("context", "delete", "integration-test-ctx");
    }

    [Fact] void should_return_success_exit_code() => _createResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_no_errors() => _createResult.StandardError.ShouldEqual(string.Empty);
}
