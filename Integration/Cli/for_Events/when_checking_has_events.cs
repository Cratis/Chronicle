// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_Events.when_checking_has_events.context;

namespace Cratis.Chronicle.Integration.Cli.for_Events;

[Collection(ChronicleCollection.Name)]
public class when_checking_has_events(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync("events", "has", "00000000-0000-0000-0000-000000000001", "--event-store", "system");
    }

    [Fact] void should_return_success_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_output() => (Context.Result.StandardOutput.Length > 0).ShouldBeTrue();

    [Fact] void should_have_no_errors() => Context.Result.StandardError.ShouldEqual(string.Empty);
}
