// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Cli;
using context = Cratis.Chronicle.Integration.Cli.for_EventTypes.when_showing_event_type.context;

namespace Cratis.Chronicle.Integration.Cli.for_EventTypes;

[Collection(ChronicleCollection.Name)]
public class when_showing_event_type(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public string EventTypeId = string.Empty;
        public CliCommandResult Result = null!;

        async Task Because()
        {
            var listResult = await RunCliAsync("event-types", "list", "--event-store", "system");
            var items = JsonDocument.Parse(listResult.StandardOutput).RootElement;
            var first = items.EnumerateArray().First();
            var eventType = first.GetProperty("type");
            EventTypeId = eventType.GetProperty("id").GetString()!;
            var generation = eventType.GetProperty("generation").GetUInt32();

            Result = await RunCliAsync("event-types", "show", $"{EventTypeId}+{generation}", "--event-store", "system");
        }
    }

    [Fact] void should_return_success_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_event_type_id() => Context.Result.StandardOutput.Contains(Context.EventTypeId).ShouldBeTrue();

    [Fact] void should_have_no_errors() => Context.Result.StandardError.ShouldEqual(string.Empty);
}
