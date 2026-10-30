// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Sequences;
using context = Cratis.Chronicle.Integration.Api.for_Sequences.when_appending_to_the_system_event_store.context;

namespace Cratis.Chronicle.Integration.Api.for_Sequences;

[Collection(ChronicleCollection.Name)]
public class when_appending_to_the_system_event_store(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public CommandResult<object> AppendResult;
        public CommandResult<object> AppendManyResult;
        public CommandResult<object> ReviseResult;
        public QueryResult Stored;

        async Task Because()
        {
            var source = Guid.NewGuid().ToString();
            var eventType = new EventType("SomeEvent", 1, false);
            AppendResult = await Client.ExecuteCommand("/api/sequences/append", new Append(
                "System", "default", "event-log", source, "Default", "All", "Default", eventType, "{}"));
            AppendManyResult = await Client.ExecuteCommand("/api/sequences/append-many", new AppendMany(
                "System", "default", "event-log", source, [new EventToAppend(eventType, "{}")]));
            ReviseResult = await Client.ExecuteCommand("/api/sequences/revise", new Revise(
                "System", "default", "event-log", 0, eventType, "{}"));
            Stored = await Client.ExecuteQuery<IEnumerable<AppendedEvent>>(
                $"/api/sequences/query-events?eventStore=System&namespace=default&eventSequenceId=event-log&eventSourceId={source}");
        }
    }

    [Fact] void should_reject_append() => Context.AppendResult.IsValid.ShouldBeFalse();
    [Fact] void should_reject_append_many() => Context.AppendManyResult.IsValid.ShouldBeFalse();
    [Fact] void should_reject_revise() => Context.ReviseResult.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_append_member() => Context.AppendResult.ValidationResults.SelectMany(result => result.Members).ShouldContain("eventStore");
    [Fact] void should_report_the_append_many_member() => Context.AppendManyResult.ValidationResults.SelectMany(result => result.Members).ShouldContain("eventStore");
    [Fact] void should_report_the_revise_member() => Context.ReviseResult.ValidationResults.SelectMany(result => result.Members).ShouldContain("eventStore");
    [Fact] void should_not_throw_on_append() => Context.AppendResult.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_throw_on_append_many() => Context.AppendManyResult.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_throw_on_revise() => Context.ReviseResult.HasExceptions.ShouldBeFalse();
    [Fact] void should_allow_reading_the_system_event_store() => Context.Stored.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_append_any_events() => (Context.Stored.Data as IEnumerable<AppendedEvent>).ShouldBeEmpty();
}
