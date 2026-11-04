// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Cratis.Traces;

namespace Cratis.Chronicle.Security.for_SystemStoreQueryFilter;

public class when_a_failing_validation_is_discovered_before_it : given.a_query_filter
{
    QueryFilters _filters;

    void Establish()
    {
        _caller = CallerNamed(Application);
        _filters = new(new DiscoveredFilters([new FailingValidation(), _filter]), new ActivitySource<QueryFilters>());
    }

    async Task Because() => _result = await _filters.OnPerform(
        ContextFor("Cratis.Chronicle.Sequences.AppendedEvent.QueryEvents", ("eventStore", "SYSTEM")));

    [Fact] void should_refuse_the_query() => _result!.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_reach_the_validation() => _result!.ValidationResults.ShouldBeEmpty();

    class FailingValidation : IQueryFilter
    {
        public Task<QueryResult> OnPerform(QueryContext context)
        {
            var result = QueryResult.Success(context.CorrelationId);
            result.ValidationResults = [ValidationResult.Error("Event store does not exist.", ["eventStore"])];
            return Task.FromResult(result);
        }
    }

    class DiscoveredFilters(IEnumerable<IQueryFilter> filters) : IInstancesOf<IQueryFilter>
    {
        public IEnumerator<IQueryFilter> GetEnumerator() => filters.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
