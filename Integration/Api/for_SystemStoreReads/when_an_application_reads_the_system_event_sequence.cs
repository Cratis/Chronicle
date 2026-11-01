// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Sequences;
using context = Cratis.Chronicle.Integration.Api.for_SystemStoreReads.when_an_application_reads_the_system_event_sequence.context;

namespace Cratis.Chronicle.Integration.Api.for_SystemStoreReads;

[Collection(ChronicleCollection.Name)]
public class when_an_application_reads_the_system_event_sequence(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public QueryResult Result;
        public QueryResult Other;

        async Task Because()
        {
            Result = await Client.ExecuteQuery<IEnumerable<AppendedEvent>>(
                "/api/sequences/query-events?eventStore=SYSTEM&namespace=default&eventSequenceId=event-log&eventSourceId=any");
            Other = await Client.ExecuteQuery<IEnumerable<AppendedEvent>>(
                "/api/sequences/query-events?eventStore=testing&namespace=default&eventSequenceId=event-log&eventSourceId=any");
        }
    }

    [Fact] void should_be_unauthorized() => Context.Result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_return_data() => (Context.Result.Data as IEnumerable<AppendedEvent>)?.ShouldBeEmpty();
    [Fact] void should_leave_other_stores_authorized() => Context.Other.IsAuthorized.ShouldBeTrue();
}
