// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_ReadModels.when_listing_read_models.context;

namespace Cratis.Chronicle.Integration.Cli.for_ReadModels;

[Collection(ChronicleCollection.Name)]
public class when_listing_read_models(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync("read-models", "list", "--event-store", "system");
    }

    [Fact] void should_return_success_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_output() => (Context.Result.StandardOutput.Length > 0).ShouldBeTrue();

    [Fact] void should_have_no_errors() => Context.Result.StandardError.ShouldEqual(string.Empty);
}
