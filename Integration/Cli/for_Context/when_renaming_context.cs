// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;

namespace Cratis.Chronicle.Integration.Cli.for_Context;

[Collection(ChronicleCollection.Name)]
public class when_renaming_context : Specification
{
    CliCommandResult _createResult = null!;
    CliCommandResult _renameResult = null!;
    CliCommandResult _deleteResult = null!;

    async Task Because()
    {
        _createResult = await CliCommandRunner.RunAsync(
            "context",
            "create",
            "integration-test-ren",
            "--server",
            "chronicle://localhost:35000/?disableTls=true");

        _renameResult = await CliCommandRunner.RunAsync("context", "rename", "integration-test-ren", "integration-test-renamed", "--output", "json");

        // Clean up.
        _deleteResult = await CliCommandRunner.RunAsync("context", "delete", "integration-test-renamed");
    }

    [Fact] void should_create_successfully() => _createResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_rename_successfully() => _renameResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_no_errors_on_rename() => _renameResult.StandardError.ShouldEqual(string.Empty);
}
