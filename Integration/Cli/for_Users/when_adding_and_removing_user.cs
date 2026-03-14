// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_Users.when_adding_and_removing_user.context;

namespace Cratis.Chronicle.Integration.Cli.for_Users;

[Collection(ChronicleCollection.Name)]
public class when_adding_and_removing_user(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult AddResult = null!;
        public CliCommandResult RemoveResult = null!;
        public bool UserAppearedInList;

        async Task Because()
        {
            AddResult = await RunCliAsync("users", "add", "integration-test-user", "integration-test@test.com", "TestP@ss123!");

            var listResult = await RunCliAsync("users", "list");
            var users = JsonDocument.Parse(listResult.StandardOutput).RootElement;
            var testUser = users.EnumerateArray()
                .FirstOrDefault(u => u.GetProperty("username").GetString() == "integration-test-user");
            UserAppearedInList = testUser.ValueKind != JsonValueKind.Undefined;

            if (UserAppearedInList)
            {
                var userId = testUser.GetProperty("id").GetString()!;
                RemoveResult = await RunCliAsync("users", "remove", userId);
            }
        }
    }

    [Fact] void should_return_success_for_add() => Context.AddResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_added_message() => Context.AddResult.StandardOutput.ShouldContain("added");

    [Fact] void should_show_user_in_list() => Context.UserAppearedInList.ShouldBeTrue();

    [Fact] void should_return_success_for_remove() => Context.RemoveResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_removed_message() => Context.RemoveResult.StandardOutput.ShouldContain("removed");
}
