// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Sequences;
using context = Cratis.Chronicle.Integration.Api.for_SystemStoreReads.when_an_application_reads_a_namespace_with_a_separator.context;

namespace Cratis.Chronicle.Integration.Api.for_SystemStoreReads;

[Collection(ChronicleCollection.Name)]
public class when_an_application_reads_a_namespace_with_a_separator(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public QueryResult Result;

        async Task Because() => Result = await Client.ExecuteQuery<IEnumerable<AppendedEvent>>(
            "/api/sequences/query-events?eventStore=testing&namespace=default%23System&eventSequenceId=event-log&eventSourceId=any");
    }

    [Fact] void should_be_a_validation_failure() => Context.Result.IsValid.ShouldBeFalse();
    [Fact] void should_not_return_data() => (Context.Result.Data as IEnumerable<AppendedEvent>)?.ShouldBeEmpty();
}
