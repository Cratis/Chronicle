// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Chronicle.Queries.for_QueryResult.when_round_tripping;

public class with_successive_collections : given.collection_query_results
{
    QueryResult<IEnumerable<AppendedEventResponse>> _first;
    QueryResult<IEnumerable<AppendedEventResponse>> _second;
    QueryResult<IEnumerable<AppendedEventResponse>> _empty;

    void Because()
    {
        _first = RoundTrip(new AppendedEventResponse { Content = """{"name":"first"}""", Context = new() { Subject = "first-subject" } });
        _second = RoundTrip(new AppendedEventResponse { Content = """{"name":"second"}""", Context = new() { Subject = "second-subject" } });
        _empty = RoundTrip();
    }

    [Fact] void should_keep_only_the_first_content_in_the_first_result() => _first.Data.Select(_ => _.Content).ShouldEqual(["""{"name":"first"}"""]);
    [Fact] void should_keep_only_the_first_subject_in_the_first_result() => _first.Data.Select(_ => _.Context.Subject).ShouldEqual(["first-subject"]);
    [Fact] void should_keep_only_the_second_content_in_the_second_result() => _second.Data.Select(_ => _.Content).ShouldEqual(["""{"name":"second"}"""]);
    [Fact] void should_keep_only_the_second_subject_in_the_second_result() => _second.Data.Select(_ => _.Context.Subject).ShouldEqual(["second-subject"]);
    [Fact] void should_return_an_empty_collection_after_populated_results() => _empty.Data.ShouldBeEmpty();
}
