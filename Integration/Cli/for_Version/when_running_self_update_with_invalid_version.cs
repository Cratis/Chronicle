// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_Version.when_running_self_update_with_invalid_version.context;

namespace Cratis.Chronicle.Integration.Cli.for_Version;

[Collection(ChronicleCollection.Name)]
public class when_running_self_update_with_invalid_version(context context) : CliGiven<context>(context)
{
    public class context : Specification
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await CliCommandRunner.RunAsync("update", "--version", "99999.0.0-nonexistent", "--output", "json");
    }

    [Fact] void should_not_return_success() => Context.Result.ExitCode.ShouldNotEqual(ExitCodes.Success);
}
