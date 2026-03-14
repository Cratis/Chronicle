// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_ReadModels.when_getting_read_model_snapshots.context;

namespace Cratis.Chronicle.Integration.Cli.for_ReadModels;

[Collection(ChronicleCollection.Name)]
public class when_getting_read_model_snapshots(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync("read-models", "snapshots", "nonexistent-read-model", "nonexistent-key", "--event-store", "system");
    }

    [Fact] void should_return_success_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_no_snapshots_message() => Context.Result.StandardOutput.ShouldContain("No snapshots found");
}
