// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_Version.when_getting_version.context;

namespace Cratis.Chronicle.Integration.Cli.for_Version;

[Collection(ChronicleCollection.Name)]
public class when_getting_version(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync("version");
    }

    [Fact] void should_return_success() => Context.Result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_output_valid_json() => JsonDocument.Parse(Context.Result.StandardOutput).ShouldNotBeNull();

    [Fact] void should_contain_cli_version() => Context.Result.StandardOutput.ShouldContain("\"version\"");

    [Fact] void should_contain_contracts_version() => Context.Result.StandardOutput.ShouldContain("\"contractsVersion\"");

    [Fact] void should_contain_server_info() => Context.Result.StandardOutput.ShouldContain("\"server\"");

    [Fact] void should_contain_compatible_field() => Context.Result.StandardOutput.ShouldContain("\"compatible\"");

    [Fact] void should_have_no_errors() => Context.Result.StandardError.Length.ShouldEqual(0);
}
