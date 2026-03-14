// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;

namespace Cratis.Chronicle.Integration.Cli.for_Auth;

[Collection(ChronicleCollection.Name)]
public class when_logging_out : Specification
{
    CliCommandResult _result = null!;

    async Task Because() => _result = await CliCommandRunner.RunAsync("logout");

    [Fact] void should_return_success_exit_code() => _result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_no_errors() => _result.StandardError.ShouldEqual(string.Empty);
}
