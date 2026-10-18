// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_a_derived_target_and_canonical_contributor : given.a_trusted_commit
{
    IUnitOfWorkEnrollmentContributor _contributor;
    ConcurrencyScope _required;
    BookId _source;

    void Establish()
    {
        _source = new("source");
        _required = new(23, new EventSourceId("source"));
        _contributor = Substitute.For<IUnitOfWorkEnrollmentContributor>();
        _contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(ConcurrencyScope.NotSet);
        _contributor.GetRequiredScope(Arg.Is<UnitOfWorkEnrollment>(_ => _.EventSourceId == new EventSourceId("source"))).Returns(_required);
        _unit.AddEnrollmentContributor(_contributor);
    }

    Task Because() => Execute(async () =>
    {
        _unit.AddEvents(EventSequenceId.Log, [new(_source, "event")], [new("source", ConcurrencyScope.NotSet)]);
        await Commit();
    });

    [Fact] void should_commit_successfully() => _error.ShouldBeNull();
    [Fact] void should_consult_the_contributor_with_the_canonical_target() => _contributor.Received(1).GetRequiredScope(Arg.Is<UnitOfWorkEnrollment>(_ => _.EventSourceId == new EventSourceId("source") && _.Events.Count == 1));
    [Fact] void should_notify_the_canonical_enrollment() => _contributor.Received(1).Enrolled(Arg.Is<UnitOfWorkEnrollment>(_ => _.EventSourceId == new EventSourceId("source") && _.Events.Count == 1), _required);
    [Fact] void should_send_the_required_guard() => _request.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(23UL);

    record BookId(string Value) : EventSourceId(Value);
}
