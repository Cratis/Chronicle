// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;

namespace Cratis.Chronicle.Integration.Cli.for_Context;

[Collection(ChronicleCollection.Name)]
public class when_setting_context : Specification
{
    CliCommandResult _createResult = null!;
    CliCommandResult _setResult = null!;
    CliCommandResult _deleteResult = null!;

    async Task Because()
    {
        _createResult = await CliCommandRunner.RunAsync(
            "context",
            "create",
            "integration-test-set",
            "--server",
            "chronicle://localhost:35000/?disableTls=true");

        _setResult = await CliCommandRunner.RunAsync("context", "set", "integration-test-set", "--output", "json");

        // Clean up: switch back to default and delete.
        await CliCommandRunner.RunAsync("context", "set", "default");
        _deleteResult = await CliCommandRunner.RunAsync("context", "delete", "integration-test-set");
    }

    [Fact] void should_create_successfully() => _createResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_set_successfully() => _setResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_no_errors_on_set() => _setResult.StandardError.ShouldEqual(string.Empty);
}
