// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_Applications.when_rotating_application_secret.context;

namespace Cratis.Chronicle.Integration.Cli.for_Applications;

[Collection(ChronicleCollection.Name)]
public class when_rotating_application_secret(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult AddResult = null!;
        public CliCommandResult RotateResult = null!;
        public CliCommandResult RemoveResult = null!;

        async Task Because()
        {
            AddResult = await RunCliAsync("applications", "add", "rotate-test-app", "initial-secret");

            var listResult = await RunCliAsync("applications", "list");
            var apps = JsonDocument.Parse(listResult.StandardOutput).RootElement;
            var testApp = apps.EnumerateArray()
                .First(a => a.GetProperty("clientId").GetString() == "rotate-test-app");
            var appId = testApp.GetProperty("id").GetString()!;

            RotateResult = await RunCliAsync("applications", "rotate-secret", appId, "new-rotated-secret");
            RemoveResult = await RunCliAsync("applications", "remove", appId);
        }
    }

    [Fact] void should_return_success_for_add() => Context.AddResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_return_success_for_rotate() => Context.RotateResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_rotated_message() => Context.RotateResult.StandardOutput.ShouldContain("rotated");

    [Fact] void should_return_success_for_cleanup() => Context.RemoveResult.ExitCode.ShouldEqual(ExitCodes.Success);
}
