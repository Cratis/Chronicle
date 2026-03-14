// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_Applications.when_adding_and_removing_application.context;

namespace Cratis.Chronicle.Integration.Cli.for_Applications;

[Collection(ChronicleCollection.Name)]
public class when_adding_and_removing_application(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult AddResult = null!;
        public CliCommandResult RemoveResult = null!;
        public bool ApplicationAppearedInList;

        async Task Because()
        {
            AddResult = await RunCliAsync("applications", "add", "integration-test-app", "integration-test-secret");

            var listResult = await RunCliAsync("applications", "list");
            var apps = JsonDocument.Parse(listResult.StandardOutput).RootElement;
            var testApp = apps.EnumerateArray()
                .FirstOrDefault(a => a.GetProperty("clientId").GetString() == "integration-test-app");
            ApplicationAppearedInList = testApp.ValueKind != JsonValueKind.Undefined;

            if (ApplicationAppearedInList)
            {
                var appId = testApp.GetProperty("id").GetString()!;
                RemoveResult = await RunCliAsync("applications", "remove", appId);
            }
        }
    }

    [Fact] void should_return_success_for_add() => Context.AddResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_added_message() => Context.AddResult.StandardOutput.ShouldContain("added");

    [Fact] void should_show_application_in_list() => Context.ApplicationAppearedInList.ShouldBeTrue();

    [Fact] void should_return_success_for_remove() => Context.RemoveResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_removed_message() => Context.RemoveResult.StandardOutput.ShouldContain("removed");
}
