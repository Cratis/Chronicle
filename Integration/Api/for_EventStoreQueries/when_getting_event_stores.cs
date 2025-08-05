// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Queries;
using context = Cratis.Chronicle.Integration.Api.for_EventStoreQueries.when_getting_event_stores.context;

namespace Cratis.Chronicle.Integration.Api.for_EventStoreQueries;

[Collection(ChronicleCollection.Name)]
public class when_getting_event_stores(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.a_configured_http_client(fixture)
    {
        public QueryResult<IEnumerable<string>> Result;

        async Task Because()
        {
            Result = await Client.ExecuteQuery<IEnumerable<string>>("/api/event-stores");
        }
    }

    [Fact]
    void should_succeed_query() => Context.Result.IsSuccess.ShouldBeTrue();

    [Fact]
    void should_return_two_event_stores_including_system() => Context.Result.Data.ShouldContainOnly(EventStoreName.System.Value, "testing");
}
