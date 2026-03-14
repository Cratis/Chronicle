// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_Projections.when_showing_projection.context;

namespace Cratis.Chronicle.Integration.Cli.for_Projections;

[Collection(ChronicleCollection.Name)]
public class when_showing_projection(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public string ListOutput = string.Empty;
        public CliCommandResult ListResult = null!;
        public CliCommandResult ShowResult = null!;

        async Task Because()
        {
            ListResult = await RunCliAsync("projections", "list", "--event-store", "system");
            ListOutput = ListResult.StandardOutput;

            var items = JsonDocument.Parse(ListOutput).RootElement;
            if (items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
            {
                var identifier = items.EnumerateArray().First().GetProperty("identifier").GetString()!;
                ShowResult = await RunCliAsync("projections", "show", identifier, "--event-store", "system");
            }
        }
    }

    [Fact] void should_return_success_for_list() => Context.ListResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_list_output() => (Context.ListOutput.Length > 0).ShouldBeTrue();

    [Fact] void should_have_no_list_errors() => Context.ListResult.StandardError.ShouldEqual(string.Empty);
}
