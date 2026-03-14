// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;

namespace Cratis.Chronicle.Integration.Cli.for_Config;

[Collection(ChronicleCollection.Name)]
public class when_showing_config_path : Specification
{
    CliCommandResult _result = null!;

    async Task Because() => _result = await CliCommandRunner.RunAsync("config", "path");

    [Fact] void should_return_success_exit_code() => _result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_output_a_file_path() => (_result.StandardOutput.Trim().Length > 0).ShouldBeTrue();

    [Fact] void should_output_cratis_directory() => _result.StandardOutput.Contains("cratis").ShouldBeTrue();
}
