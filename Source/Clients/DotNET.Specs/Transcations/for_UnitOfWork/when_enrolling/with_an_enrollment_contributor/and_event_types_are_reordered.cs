// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_event_types_are_reordered : given.a_contributed_unit
{
    ConcurrencyScope _supplied;

    void Establish()
    {
        var first = new EventType("first", EventTypeGeneration.First);
        var second = new EventType("second", EventTypeGeneration.First);
        _required = _required with { EventTypes = [first, second] };
        _contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(_required);
        _supplied = _required with { EventTypes = [second, first, second] };
    }

    async Task Because()
    {
        Stage(_supplied);
        await _unitOfWork.Commit();
    }

    [Fact] void should_compare_the_filter_as_a_set() => _concurrencyScopesAppended[_source].EventTypes.ShouldContainOnly(_supplied.EventTypes!);
}
