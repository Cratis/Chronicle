// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_Observers.when_replaying_observer.context;

namespace Cratis.Chronicle.Integration.Cli.for_Observers;

[Collection(ChronicleCollection.Name)]
public class when_replaying_observer(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public string ObserverId = string.Empty;
        public CliCommandResult Result = null!;

        async Task Because()
        {
            var listResult = await RunCliAsync("observers", "list", "--event-store", "system");
            var items = JsonDocument.Parse(listResult.StandardOutput).RootElement;
            ObserverId = items.EnumerateArray().First().GetProperty("id").GetString()!;

            Result = await RunCliAsync("observers", "replay", ObserverId, "--event-store", "system");
        }
    }

    [Fact] void should_return_success_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_replay_started_message() => Context.Result.StandardOutput.ShouldContain("Replay started");

    [Fact] void should_have_no_errors() => Context.Result.StandardError.ShouldEqual(string.Empty);
}
