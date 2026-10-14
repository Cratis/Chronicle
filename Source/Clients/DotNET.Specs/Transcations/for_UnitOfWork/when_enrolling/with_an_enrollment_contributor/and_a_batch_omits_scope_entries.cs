// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_a_batch_omits_scope_entries : given.a_contributed_unit
{
    EventSourceId _other;

    void Establish()
    {
        _other = EventSourceId.New();
        _contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(call => new ConcurrencyScope(0UL, call.Arg<UnitOfWorkEnrollment>().EventSourceId));
    }

    async Task Because()
    {
        _unitOfWork.AddEvents(EventSequenceId.Log, [new(_source, _event), new(_other, new object()), new(_source, new object())], []);
        await _unitOfWork.Commit();
    }

    [Fact] void should_consult_each_distinct_target() => _contributor.Received(2).GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>());
    [Fact] void should_describe_missing_scopes_as_not_set() => _contributor.Received(2).GetRequiredScope(Arg.Is<UnitOfWorkEnrollment>(_ => _.SuppliedScope == ConcurrencyScope.NotSet));
    [Fact] void should_group_events_for_the_target() => _contributor.Received(1).GetRequiredScope(Arg.Is<UnitOfWorkEnrollment>(_ => _.EventSourceId == _source && _.Events.Count == 2));
    [Fact] void should_enroll_the_scope_for_the_second_target() => _concurrencyScopesAppended[_other].ShouldEqual(new ConcurrencyScope(0UL, _other));
}
