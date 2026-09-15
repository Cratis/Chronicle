// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.EventStores;
using Cratis.Chronicle.Sequences;
using context = Cratis.Chronicle.Integration.Api.for_Sequences.when_querying_an_empty_sequence.context;

namespace Cratis.Chronicle.Integration.Api.for_Sequences;

[Collection(ChronicleCollection.Name)]
public class when_querying_an_empty_sequence(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public QueryResult Result;
        public IEnumerable<AppendedEvent> Data;

        Task Establish() => Client.ExecuteCommand("/api/event-stores/ensure-event-store", new EnsureEventStore("empty-sequence-store"));

        async Task Because()
        {
            Result = await Client.ExecuteQuery<IEnumerable<AppendedEvent>>(
                "/api/sequences/query-events?eventStore=empty-sequence-store&namespace=default&eventSequenceId=event-log");
            Data = Result.Data as IEnumerable<AppendedEvent>;
        }
    }

    [Fact] void should_succeed_query() => Context.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_an_empty_collection() => Context.Data.ShouldBeEmpty();
}
