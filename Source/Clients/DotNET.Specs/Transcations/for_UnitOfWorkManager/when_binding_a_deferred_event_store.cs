// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_binding_a_deferred_event_store : Specification
{
    UnitOfWorkManager _manager;
    IEventStore _firstStore;
    IEventStore _secondStore;
    IEventStore _selectedStore;
    IUnitOfWork _unit;
    int _resolutions;
    int _resolutionsAfterBegin;

    void Establish()
    {
        _firstStore = Substitute.For<IEventStore>();
        _secondStore = Substitute.For<IEventStore>();
        IEventStore Resolve()
        {
            _resolutions++;
            return _selectedStore;
        }

        _manager = new UnitOfWorkManager(Resolve, null, UnitOfWorkLifecyclePolicy.Compatibility, null);
    }

    async Task Because()
    {
        _unit = _manager.Begin(CorrelationId.New());
        _resolutionsAfterBegin = _resolutions;
        _selectedStore = _firstStore;
        _unit.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
        _selectedStore = _secondStore;
        _unit.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
        await _unit.Rollback();
    }

    [Fact] void should_not_resolve_the_store_on_begin() => _resolutionsAfterBegin.ShouldEqual(0);
    [Fact] void should_bind_only_once() => _resolutions.ShouldEqual(1);
    [Fact] void should_use_the_first_selected_store() => _firstStore.Received().GetEventSequence(EventSequenceId.Log);
    [Fact] void should_never_use_the_second_store() => _secondStore.DidNotReceive().GetEventSequence(Arg.Any<EventSequenceId>());
    [Fact] void should_remove_the_completed_unit() => _manager.TryGetFor(_unit.CorrelationId, out _).ShouldBeFalse();
}
