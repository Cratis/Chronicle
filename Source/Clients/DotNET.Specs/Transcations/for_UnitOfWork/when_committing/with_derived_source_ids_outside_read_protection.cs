// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_derived_source_ids_outside_read_protection : given.a_unit_of_work
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_caller_ids_grouping_and_order(bool ordered)
    {
        EventSourceId[] sources = [new BookId("book"), new("other"), new("book")];
        object[] events = [new object(), new object(), new object()];
        var contributor = Substitute.For<IUnitOfWorkEnrollmentContributor>();
        contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(ConcurrencyScope.NotSet);
        using var registration = _unitOfWork.AddEnrollmentContributor(contributor);
        if (ordered)
        {
            _unitOfWork.AddEvents(EventSequenceId.Log, sources.Select((source, index) => new EventForEventSourceId(source, events[index])), []);
        }
        else
        {
            for (var index = 0; index < sources.Length; index++)
            {
                _unitOfWork.AddEvent(EventSequenceId.Log, sources[index], events[index], Causation.Unknown());
            }
        }
        await _unitOfWork.Commit();

        var appended = _eventsAppended.ToArray();
        for (var index = 0; index < sources.Length; index++)
        {
            Assert.Same(sources[index], appended[index].EventSourceId);
            Assert.Same(events[index], appended[index].Event);
            contributor.Received(1).GetRequiredScope(Arg.Is<UnitOfWorkEnrollment>(enrollment => ReferenceEquals(enrollment.EventSourceId, sources[index]) && enrollment.Events.Count == 1));
            contributor.Received(1).Enrolled(Arg.Is<UnitOfWorkEnrollment>(enrollment => ReferenceEquals(enrollment.EventSourceId, sources[index])), ConcurrencyScope.NotSet);
        }
        _concurrencyScopesAppended.Count.ShouldEqual(0);
    }

    [Fact]
    public async Task should_preserve_explicit_scope_ids_across_ordered_batches()
    {
        var source = new BookId("book");
        var scope = new ConcurrencyScope(0, source);
        _unitOfWork.AddEvents(EventSequenceId.Log, [new(source, new object())], [new(source, scope)]);
        _unitOfWork.AddEvents(EventSequenceId.Log, [new(source, new object())], []);
        await _unitOfWork.Commit();

        Assert.Same(source, _concurrencyScopesAppended.Keys.Single());
        Assert.Same(source, _concurrencyScopesAppended.Values.Single().EventSourceId);
        Assert.All(_eventsAppended, entry => Assert.Same(source, entry.EventSourceId));
    }

    record BookId(string Value) : EventSourceId(Value);
}
