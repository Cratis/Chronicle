// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;

namespace Cratis.Chronicle.Integration.Cli.for_Context;

[Collection(ChronicleCollection.Name)]
public class when_showing_context : Specification
{
    CliCommandResult _result = null!;

    async Task Because() => _result = await CliCommandRunner.RunAsync("context", "show", "--output", "json");

    [Fact] void should_return_success_exit_code() => _result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_output() => (_result.StandardOutput.Length > 0).ShouldBeTrue();

    [Fact] void should_have_no_errors() => _result.StandardError.ShouldEqual(string.Empty);
}
