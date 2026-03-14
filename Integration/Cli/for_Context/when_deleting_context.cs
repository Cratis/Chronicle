// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;

namespace Cratis.Chronicle.Integration.Cli.for_Context;

[Collection(ChronicleCollection.Name)]
public class when_deleting_context : Specification
{
    CliCommandResult _createResult = null!;
    CliCommandResult _deleteResult = null!;

    async Task Because()
    {
        _createResult = await CliCommandRunner.RunAsync(
            "context",
            "create",
            "integration-test-del",
            "--server",
            "chronicle://localhost:35000/?disableTls=true");

        _deleteResult = await CliCommandRunner.RunAsync("context", "delete", "integration-test-del", "--output", "json");
    }

    [Fact] void should_create_successfully() => _createResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_delete_successfully() => _deleteResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_no_errors_on_delete() => _deleteResult.StandardError.ShouldEqual(string.Empty);
}
