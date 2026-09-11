// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Chronicle.Queries.for_QueryResult.when_round_tripping;

public class with_concurrent_collections : given.collection_query_results
{
    QueryResult<IEnumerable<AppendedEventResponse>>[] _results;
    QueryResult<IEnumerable<AppendedEventResponse>> _empty;

    async Task Because()
    {
        _results = await Task.WhenAll(Enumerable.Range(0, 8).Select(index => Task.Run(() => RoundTrip(new AppendedEventResponse
        {
            Content = $$"""{"index":{{index}}}""",
            Context = new() { Subject = $"subject-{index}" }
        })))).WaitAsync(TimeSpan.FromSeconds(10));
        _empty = RoundTrip();
    }

    [Fact] void should_keep_each_results_own_content() => _results.Select(_ => _.Data.Single().Content).ShouldEqual(Enumerable.Range(0, 8).Select(index => $$"""{"index":{{index}}}"""));
    [Fact] void should_keep_each_results_own_subject() => _results.Select(_ => _.Data.Single().Context.Subject).ShouldEqual(Enumerable.Range(0, 8).Select(index => $"subject-{index}"));
    [Fact] void should_return_an_empty_collection_after_concurrent_results() => _empty.Data.ShouldBeEmpty();
}
