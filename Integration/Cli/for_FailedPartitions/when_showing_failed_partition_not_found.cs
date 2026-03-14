// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_FailedPartitions.when_showing_failed_partition_not_found.context;

namespace Cratis.Chronicle.Integration.Cli.for_FailedPartitions;

[Collection(ChronicleCollection.Name)]
public class when_showing_failed_partition_not_found(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync(
            "failed-partitions",
            "show",
            "00000000-0000-0000-0000-000000000099",
            "nonexistent-partition",
            "--event-store",
            "system");
    }

    [Fact] void should_return_not_found_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.NotFound);

    [Fact] void should_have_error_output() => (Context.Result.StandardOutput.Length + Context.Result.StandardError.Length > 0).ShouldBeTrue();
}
