// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_TransactionalEventSequence.when_appending;

public class with_an_enrollment_contributor : given.a_transactional_event_sequence
{
    UnitOfWork _realUnit;
    IUnitOfWorkEnrollmentContributor _contributor;
    EventSourceId _source;
    object _event;
    ConcurrencyScope _required;

    void Establish()
    {
        _realUnit = new(CorrelationId.New(), _ => { }, Substitute.For<IEventStore>());
        _unitOfWorkManager.Current.Returns(_realUnit);
        _source = EventSourceId.New();
        _event = new SomeEvent("contributed");
        _required = new(EventSequenceNumber.BeforeFirst, _source);
        _contributor = Substitute.For<IUnitOfWorkEnrollmentContributor>();
        _contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(_required);
        _realUnit.AddEnrollmentContributor(_contributor);
    }

    Task Because() => _transactionalEventSequence.Append(_source, _event);

    [Fact] void should_consult_the_real_units_contributor() => _contributor.Received(1).GetRequiredScope(Arg.Is<UnitOfWorkEnrollment>(_ => _.EventSourceId == _source && _.Events.Single() == _event));
    [Fact] void should_stage_the_transactional_event() => _realUnit.GetEvents().ShouldContainOnly(_event);
    [Fact] void should_notify_with_the_required_scope() => _contributor.Received(1).Enrolled(Arg.Any<UnitOfWorkEnrollment>(), _required);
}
