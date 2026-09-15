// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Chronicle.Queries.for_QueryResult.when_mutating_a_collection;

public class with_another_query_result : Specification
{
    QueryResult<IEnumerable<AppendedEventResponse>> _first;
    QueryResult<IEnumerable<AppendedEventResponse>> _second;
    QueryResult<IEnumerable<AppendedEventResponse>> _createdAfterMutation;
    AppendedEventResponse _event;

    void Establish()
    {
        _first = new();
        _second = new();
        _event = new() { Content = """{"name":"added"}""", Context = new() { Subject = "added-subject" } };
    }

    void Because()
    {
        ((ICollection<AppendedEventResponse>)_first.Data).Add(_event);
        _createdAfterMutation = new();
    }

    [Fact] void should_add_the_event_to_the_first_result() => _first.Data.ShouldEqual([_event]);
    [Fact] void should_not_change_another_existing_result() => _second.Data.ShouldBeEmpty();
    [Fact] void should_not_change_the_default_for_later_results() => _createdAfterMutation.Data.ShouldBeEmpty();
}
