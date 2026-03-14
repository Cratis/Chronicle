// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_Recommendations.when_performing_nonexistent_recommendation.context;

namespace Cratis.Chronicle.Integration.Cli.for_Recommendations;

[Collection(ChronicleCollection.Name)]
public class when_performing_nonexistent_recommendation(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync("recommendations", "perform", "00000000-0000-0000-0000-000000000099", "--event-store", "system");
    }

    [Fact] void should_not_return_success() => (Context.Result.ExitCode != ExitCodes.Success).ShouldBeTrue();

    [Fact] void should_have_error_output() => (Context.Result.StandardError.Length > 0).ShouldBeTrue();
}
