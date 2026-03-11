// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_ConnectionErrors.when_connecting_to_unavailable_server.context;

namespace Cratis.Chronicle.Integration.Cli.for_ConnectionErrors;

[Collection(ChronicleCollection.Name)]
public class when_connecting_to_unavailable_server(context context) : CliGiven<context>(context)
{
    public class context : Specification
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await CliCommandRunner.RunAsync("event-stores", "list", "--server", "chronicle://dev:devsecret@localhost:19999", "--output", "json");
    }

    [Fact] void should_return_connection_error_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.ConnectionError);

    [Fact] void should_output_error_to_stderr() => Context.Result.StandardError.Contains("error").ShouldBeTrue();
}
