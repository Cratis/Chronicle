// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_ReadModels.when_getting_read_model_by_key_not_found.context;

namespace Cratis.Chronicle.Integration.Cli.for_ReadModels;

[Collection(ChronicleCollection.Name)]
public class when_getting_read_model_by_key_not_found(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync("read-models", "by-key", "nonexistent-read-model", "nonexistent-key", "--event-store", "system");
    }

    [Fact] void should_not_return_success() => Context.Result.ExitCode.ShouldNotEqual(ExitCodes.Success);

    [Fact] void should_not_have_errors() => Context.Result.StandardError.Length.ShouldEqual(0);
}
