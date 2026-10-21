// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor.given;

public class a_contributed_unit : for_UnitOfWork.given.a_unit_of_work
{
    protected EventSourceId _source;
    protected ConcurrencyScope _required;
    protected IUnitOfWorkEnrollmentContributor _contributor;
    protected IDisposable _registration;
    protected object _event;

    void Establish()
    {
        _source = EventSourceId.New();
        _required = new(EventSequenceNumber.BeforeFirst, _source);
        _event = new object();
        _contributor = Substitute.For<IUnitOfWorkEnrollmentContributor>();
        _contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(_required);
        _registration = _unitOfWork.AddEnrollmentContributor(_contributor);
    }

    protected void Stage(ConcurrencyScope? scope = null) => _unitOfWork.AddEvent(
        EventSequenceId.Log, _source, _event, Causation.Unknown(), concurrencyScope: scope);
}
