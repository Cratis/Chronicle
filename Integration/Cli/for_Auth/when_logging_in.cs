// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_Auth.when_logging_in.context;

namespace Cratis.Chronicle.Integration.Cli.for_Auth;

[Collection(ChronicleCollection.Name)]
public class when_logging_in(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult AddResult = null!;
        public CliCommandResult LoginResult = null!;
        public CliCommandResult RemoveResult = null!;

        async Task Because()
        {
            AddResult = await RunCliAsync("users", "add", "login-test-user", "login-test@test.com", "TestP@ss123!");

            LoginResult = await RunCliAsync("login", "login-test-user", "--password", "TestP@ss123!");

            var listResult = await RunCliAsync("users", "list");
            var users = JsonDocument.Parse(listResult.StandardOutput).RootElement;
            var testUser = users.EnumerateArray()
                .FirstOrDefault(u => u.GetProperty("username").GetString() == "login-test-user");
            if (testUser.ValueKind != JsonValueKind.Undefined)
            {
                var userId = testUser.GetProperty("id").GetString()!;
                RemoveResult = await RunCliAsync("users", "remove", userId);
            }
        }
    }

    [Fact] void should_add_user_successfully() => Context.AddResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_return_success_exit_code() => Context.LoginResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_logged_in_message() => Context.LoginResult.StandardOutput.ShouldContain("Logged in as");

    [Fact] void should_have_no_errors() => Context.LoginResult.StandardError.ShouldEqual(string.Empty);
}
